using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MethodOverloadGenerator.Building;
using MethodOverloadGenerator.Building.Rules;
using MethodOverloadGenerator.Generation.Rules;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Bridges a single <c>[MethodOverloadGenerator]</c> candidate (as produced by
/// <c>ForAttributeWithMetadataName</c>) to the builder/emitter pipeline, combines every
/// resolved method's output into one generated file, and reports the resulting diagnostics
/// and source back to the incremental generator driver. Contains no domain logic itself.
/// </summary>
internal static class GenerationPipeline
{
    private static readonly MasterContextBuilder Builder = new(
        new DeclarationContextBuilder(),
        new AllowedRulesContextBuilder(),
        new DelegateParametersBuilder(),
        new Rule1ContextsBuilder(),
        new Rule2ContextsBuilder(),
        new Rule3ContextsBuilder(),
        new Rule4ContextBuilder(),
        new Rule5ContextBuilder());

    private static readonly MasterContextEmitter Emitter = new(
        new DiagnosticsBuilder(),
        new Rule1Emitter(),
        new Rule2Emitter(),
        new Rule3Emitter(),
        new Rule4Emitter(),
        new Rule5Emitter());

    private static readonly PartialClassEmitter FileEmitter = new();

    /// <summary>
    /// Every method/constructor resolved from one attribute usage contributes its own region
    /// to a single generated file (one per attribute usage, not one per method) — this keeps a
    /// class-level attribute's fan-out to many methods from scattering into many near-empty
    /// files, while still letting a reader jump straight to one original method's overloads.
    /// </summary>
    public static void Execute(GeneratorAttributeSyntaxContext candidate, AnalyzerConfigOptions options, SourceProductionContext spc)
    {
        DeclarationContext? declaration = null;
        var usings = new List<string>();
        var regions = new List<string>();

        foreach (var target in ResolveTargets(candidate))
        {
            var context = Builder.Build(target.Method, target.Attribute, options, target.Placement,
                target.NonDelegateParamName, target.AttributedDelegateParamName);
            var result = Emitter.Emit(context);

            foreach (var diagnostic in result.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            if (result.MethodBodies.Count == 0)
                continue;

            declaration ??= context.Declaration;
            usings.AddRange(context.Declaration.Usings);
            regions.Add(BuildRegion(context.Declaration.MethodName, result.MethodBodies));
        }

        if (declaration is null)
            return;

        var mergedDeclaration = declaration with { Usings = usings.Distinct().ToArray() };
        var source = FileEmitter.Emit(mergedDeclaration, regions);
        spc.AddSource(HintName(candidate.TargetSymbol), source);
    }

    private static string BuildRegion(string methodName, IReadOnlyList<string> methodBodies)
        => $"#region {methodName}\n\n{string.Join("\n\n", methodBodies)}\n\n#endregion";

    /// <summary>
    /// Resolves the attributed syntax node down to the set of methods/constructors that must
    /// be processed. Class-level placement fans out to every ordinary method and constructor
    /// declared on the class; method/constructor and parameter placement each yield exactly one.
    /// </summary>
    private static IEnumerable<Target> ResolveTargets(GeneratorAttributeSyntaxContext candidate)
    {
        var attribute = candidate.Attributes[0];

        switch (candidate.TargetSymbol)
        {
            case INamedTypeSymbol type:
                foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
                    if (member.MethodKind is MethodKind.Ordinary or MethodKind.Constructor)
                        yield return new Target(member, attribute, AttributePlacement.Class, null, null);
                break;

            case IMethodSymbol method:
                yield return new Target(method, attribute, AttributePlacement.Method, null, null);
                break;

            case IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter:
                var isDelegateParam = parameter.Type.TypeKind == TypeKind.Delegate;
                var nonDelegateParamName = isDelegateParam ? null : parameter.Name;
                var attributedDelegateParamName = isDelegateParam ? parameter.Name : null;
                yield return new Target(method, attribute, AttributePlacement.Parameter, nonDelegateParamName, attributedDelegateParamName);
                break;
        }
    }

    // Keyed off the attributed symbol itself — the class for class-level placement (one file for
    // every method it fans out to), the method for method-level, and the containing method plus
    // the parameter's own name for parameter-level — rather than any one resolved method, since
    // the file now represents the whole attribute usage. Including the parameter name keeps two
    // parameter-level attribute usages on the same method from colliding on one hint name, which
    // otherwise crashes the generator (AdditionalSourcesCollection requires unique hint names).
    private static string HintName(ISymbol attributedSymbol)
    {
        if (attributedSymbol is IParameterSymbol parameter)
        {
            var containingRaw = parameter.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"{Sanitize(containingRaw)}.{Sanitize(parameter.Name)}.g.cs";
        }

        var raw = attributedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return Sanitize(raw) + ".g.cs";
    }

    private static string Sanitize(string raw)
        => new string(raw.Select(c => char.IsLetterOrDigit(c) || c is '_' or '.' ? c : '_').ToArray());

    private readonly record struct Target(
        IMethodSymbol Method,
        AttributeData Attribute,
        AttributePlacement Placement,
        string? NonDelegateParamName,
        string? AttributedDelegateParamName);
}
