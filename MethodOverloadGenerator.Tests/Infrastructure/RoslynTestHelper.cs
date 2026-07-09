using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MethodOverloadGenerator.Emission;

namespace MethodOverloadGenerator.Tests.Infrastructure;

/// <summary>
/// Creates in-memory Roslyn compilations that include the emitted attribute/enum source so
/// builder tests can work with real <see cref="IMethodSymbol"/> and <see cref="AttributeData"/>
/// instances without running the full generator pipeline.
/// </summary>
internal static class RoslynTestHelper
{

    private static readonly IReadOnlyList<MetadataReference> DefaultReferences = BuildReferences();

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var paths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
    }

    // Mirrors the implicit usings injected by the .NET SDK when <ImplicitUsings>enable</ImplicitUsings>
    // is set, so that test source strings can use Func<>, Action, Task<>, etc. unqualified — just
    // as they would in the real project that consumes the generator.
    private static readonly SyntaxTree ImplicitUsingsTree = CSharpSyntaxTree.ParseText("""
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """);

    public static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            [ImplicitUsingsTree, CSharpSyntaxTree.ParseText(AttributeEmitter.Source), CSharpSyntaxTree.ParseText(source)],
            DefaultReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Returns the first method with <paramref name="methodName"/> on the type identified by
    /// <paramref name="fullyQualifiedTypeName"/> (e.g. <c>"Foo.Bar.MyClass"</c> or <c>"MyClass"</c>).
    /// </summary>
    public static IMethodSymbol GetMethod(string source, string fullyQualifiedTypeName, string methodName)
    {
        var compilation = CreateCompilation(source);
        var type = compilation.GetTypeByMetadataName(fullyQualifiedTypeName)
            ?? throw new InvalidOperationException($"Type '{fullyQualifiedTypeName}' not found.");
        return type.GetMembers(methodName).OfType<IMethodSymbol>().First();
    }

    /// <summary>
    /// Returns the <see cref="AttributeData"/> for <c>MethodOverloadGeneratorAttribute</c>
    /// placed directly on the named method.
    /// </summary>
    public static AttributeData GetMethodOverloadAttribute(string source, string fullyQualifiedTypeName, string methodName)
    {
        var method = GetMethod(source, fullyQualifiedTypeName, methodName);
        return method.GetAttributes()
            .Single(a => a.AttributeClass?.Name == GeneratorConstants.AttributeClassName);
    }
}
