using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Emission;
using MethodOverloadGenerator.Filtering;
using MethodOverloadGenerator.Generation;

namespace MethodOverloadGenerator;

[Generator]
public class MethodOverloadGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit [MethodOverloadGenerator] + RuleOverride into every consuming compilation.
        // RegisterPostInitializationOutput runs before any syntax analysis, so the attribute
        // is resolvable when ForAttributeWithMetadataName scans the compilation.
        context.RegisterPostInitializationOutput(AttributeEmitter.Emit);

        // Generated overloads carry [OverloadResolutionPriority] so IntelliSense/overload
        // resolution ranks the original method first; polyfill it for pre-.NET 9 consumers.
        context.RegisterPostInitializationOutput(OverloadResolutionPriorityAttributeEmitter.Emit);

        // Find every syntax node that carries [MethodOverloadGenerator].
        // ForAttributeWithMetadataName handles the semantic attribute match internally;
        // the predicate only needs to guard on supported node kinds.
        // If no nodes pass, the provider is empty and nothing downstream executes —
        // this is the "exit as soon as possible" guarantee.
        IncrementalValuesProvider<GeneratorAttributeSyntaxContext> candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GeneratorConstants.AttributeMetadataName,
                predicate: CandidateSyntaxFilter.IsSupportedNode,
                transform: static (ctx, _) => ctx);

        // MSBuild property fallbacks (e.g. MethodOverloadGenerator_SyncOverloads) live on the
        // global analyzer config options, so every candidate needs them alongside its own data.
        var candidatesWithOptions = candidates.Combine(context.AnalyzerConfigOptionsProvider);

        context.RegisterSourceOutput(candidatesWithOptions, static (spc, pair) =>
            GenerationPipeline.Execute(pair.Left, pair.Right.GlobalOptions, spc));
    }
}
