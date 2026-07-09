namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Context for Rule 5 — Multiple attributed parameters → combinatorial overloads.
/// One instance per method that has two or more attributed delegate parameters.
/// </summary>
/// <remarks>
/// <see cref="AttributedParameters"/> lists every attributed parameter in declaration order.
/// The emitter computes the cross-product of each parameter's individual variants
/// (derived from Rules 1–3) to produce all combinations, minus the original signature.
/// </remarks>
internal sealed record Rule5Context
{
    /// <summary>
    /// All attributed delegate parameters of the method, in declaration order.
    /// Must contain at least two entries (the single-parameter case is handled by Rules 1–3 alone).
    /// </summary>
    public required IReadOnlyList<DelegateInfo> AttributedParameters { get; init; }
}
