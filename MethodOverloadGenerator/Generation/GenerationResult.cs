using Microsoft.CodeAnalysis;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// The output of <see cref="MasterContextEmitter.Emit"/>: zero or more diagnostics and,
/// when no errors were found, the rendered method declarations for this one method — not yet
/// wrapped in a partial-class shell, since <see cref="GenerationPipeline"/> combines the
/// method bodies from every method under the same attribute usage into a single file.
/// </summary>
internal sealed record GenerationResult
{
    /// <summary>
    /// Diagnostics (errors and warnings) produced for this method.
    /// May be non-empty even when <see cref="MethodBodies"/> is non-empty (warnings).
    /// </summary>
    public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

    /// <summary>
    /// The generated overload method declarations for this method, or empty when one or more
    /// errors in <see cref="Diagnostics"/> prevented generation, or when no rule applied.
    /// </summary>
    public required IReadOnlyList<string> MethodBodies { get; init; }
}
