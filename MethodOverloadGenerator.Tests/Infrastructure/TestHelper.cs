using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MethodOverloadGenerator.Tests.Infrastructure;

/// <summary>
/// Runs the real <c>MethodOverloadGenerator</c> against an in-memory C# compilation — unlike
/// <see cref="TestContextHelper"/> (which builds a <c>MasterContext</c> directly), this exercises
/// the whole incremental generator pipeline end to end, exactly as a consuming project would.
/// </summary>
internal static class TestHelper
{
    private static readonly IReadOnlyList<MetadataReference> References = BuildReferences();

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var paths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
    }

    // Mirrors the implicit usings injected by the .NET SDK when <ImplicitUsings>enable</ImplicitUsings>
    // is set, so test source strings can use Func<>, Action, Task<>, etc. unqualified — just as
    // they would in the real project that consumes the generator.
    private static readonly SyntaxTree ImplicitUsingsTree = CSharpSyntaxTree.ParseText("""
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """);

    // A real `dotnet build` defines NETx_0_OR_GREATER preprocessor symbols from the project's
    // TargetFramework automatically; an in-memory CSharpCompilation built by hand does not. Without
    // this, the generator's own #if !NET9_0_OR_GREATER polyfill for OverloadResolutionPriorityAttribute
    // never steps aside, and collides with the real BCL type already visible via the test host's own
    // netX.0 references (CS0436). Matches this repo's net10.0 test host.
    private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions()
        .WithPreprocessorSymbols("NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER",
                                  "NET8_0_OR_GREATER", "NET9_0_OR_GREATER", "NET10_0_OR_GREATER");

    /// <param name="source">The consuming project's source, without the [MethodOverloadGenerator]
    /// attribute definition — the real generator supplies that itself via post-initialization
    /// output, exactly as it would for an actual consumer.</param>
    /// <param name="msBuildProperties">
    /// Simulated <c>build_property.MethodOverloadGenerator_*</c> MSBuild properties, or
    /// <see langword="null"/> for none set.
    /// </param>
    public static GeneratorResult RunGenerator(string source, Dictionary<string, string>? msBuildProperties = null)
    {
        var compilation = CSharpCompilation.Create(
            "GeneratorTestAssembly",
            [ImplicitUsingsTree, CSharpSyntaxTree.ParseText(source, ParseOptions)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var optionsProvider = msBuildProperties is null
            ? TestAnalyzerConfigOptionsProvider.Empty
            : new TestAnalyzerConfigOptionsProvider(msBuildProperties);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new global::MethodOverloadGenerator.MethodOverloadGenerator().AsSourceGenerator()],
            additionalTexts: null,
            parseOptions: ParseOptions,
            optionsProvider: optionsProvider);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var generatedSources = driver.GetRunResult().GeneratedTrees
            .Where(t => !t.FilePath.EndsWith(GeneratorConstants.AttributeHintName, StringComparison.Ordinal)
                     && !t.FilePath.EndsWith(GeneratorConstants.OverloadResolutionPriorityHintName, StringComparison.Ordinal))
            .Select(t => t.ToString())
            .ToList();

        // Generator-reported diagnostics (MOG001-MOG005) aren't part of Compilation.GetDiagnostics()
        // on their own, so merge both sets — deduplicated, since a generator diagnostic can also
        // surface there depending on Roslyn version. Hidden-severity diagnostics (e.g. "unnecessary
        // using directive" IDE hints from the baseline usings PartialClassEmitter always adds) are
        // pure noise for testing generator correctness, so they're excluded entirely.
        var diagnostics = generatorDiagnostics
            .Concat(outputCompilation.GetDiagnostics())
            .Where(d => d.Severity != DiagnosticSeverity.Hidden)
            .GroupBy(d => (d.Id, d.Location, Message: d.GetMessage()))
            .Select(g => g.First())
            .ToList();

        return new GeneratorResult
        {
            GeneratedSources = generatedSources,
            Diagnostics = diagnostics,
        };
    }
}

internal sealed class GeneratorResult
{
    public IReadOnlyList<string> GeneratedSources { get; init; } = [];
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>Convenience accessor when exactly one file is expected to be generated.</summary>
    public string SingleGeneratedSource =>
        GeneratedSources.Count == 1
            ? GeneratedSources[0]
            : throw new InvalidOperationException(
                $"Expected exactly 1 generated source file but got {GeneratedSources.Count}.");

    public IEnumerable<Diagnostic> Errors =>
        Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    public IEnumerable<Diagnostic> Warnings =>
        Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
}
