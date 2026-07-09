using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Generation.Rules;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Orchestrates code generation for one <see cref="MasterContext"/>: runs diagnostics first,
/// short-circuits on errors, then delegates each rule's emission to its dedicated emitter.
/// Returns raw method declarations rather than a complete file — <see cref="GenerationPipeline"/>
/// combines the methods from every method under the same attribute usage into one partial-class
/// file via <see cref="PartialClassEmitter"/>. Contains no domain logic itself.
/// </summary>
internal sealed class MasterContextEmitter
{
    private readonly DiagnosticsBuilder _diagnosticsBuilder;
    private readonly Rule1Emitter       _rule1Emitter;
    private readonly Rule2Emitter       _rule2Emitter;
    private readonly Rule3Emitter       _rule3Emitter;
    private readonly Rule4Emitter       _rule4Emitter;
    private readonly Rule5Emitter       _rule5Emitter;

    public MasterContextEmitter(
        DiagnosticsBuilder diagnosticsBuilder,
        Rule1Emitter       rule1Emitter,
        Rule2Emitter       rule2Emitter,
        Rule3Emitter       rule3Emitter,
        Rule4Emitter       rule4Emitter,
        Rule5Emitter       rule5Emitter)
    {
        _diagnosticsBuilder = diagnosticsBuilder;
        _rule1Emitter       = rule1Emitter;
        _rule2Emitter       = rule2Emitter;
        _rule3Emitter       = rule3Emitter;
        _rule4Emitter       = rule4Emitter;
        _rule5Emitter       = rule5Emitter;
    }

    public GenerationResult Emit(MasterContext context)
    {
        var diagnostics = _diagnosticsBuilder.Build(context);

        // Errors prevent generation — consumers still receive the diagnostics to report.
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new GenerationResult { Diagnostics = diagnostics, MethodBodies = [] };

        if (!_diagnosticsBuilder.IsEligibleForGeneration(context))
            return new GenerationResult { Diagnostics = diagnostics, MethodBodies = [] };

        var methods = new List<string>();

        if (context.ApplyRule1)
            methods.AddRange(_rule1Emitter.Emit(context.Rule1Contexts!, context.Declaration));
        if (context.ApplyRule2)
            methods.AddRange(_rule2Emitter.Emit(context.Rule2Contexts!, context.Declaration));
        if (context.ApplyRule3)
            methods.AddRange(_rule3Emitter.Emit(context.Rule3Contexts!, context.Declaration));
        if (context.ApplyRule4)
            methods.AddRange(_rule4Emitter.Emit(context.Rule4Context!, context.Declaration));
        if (context.ApplyRule5)
            methods.AddRange(_rule5Emitter.Emit(context.Rule5Context!, context.Declaration, context.AllowedRules, context.Rule4Context));

        return new GenerationResult { Diagnostics = diagnostics, MethodBodies = methods };
    }
}
