using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Models;

/// <summary>
/// Top-level context for one method (or constructor) that carries the
/// <c>[MethodOverloadGenerator]</c> attribute at any level.
///
/// Contains:
/// <list type="bullet">
///   <item>A <see cref="DeclarationContext"/> with the miscellaneous structural properties
///         (namespace, class name, method name, access modifier, static, extension method).</item>
///   <item>One <c>bool</c> per rule indicating whether that rule is active for this method.</item>
///   <item>The corresponding rule context — populated when the rule's bool is <see langword="true"/>,
///         empty / <see langword="null"/> otherwise.</item>
/// </list>
/// </summary>
internal sealed record MasterContext
{
    public required DeclarationContext Declaration { get; init; }

    public required AllowedRulesContext AllowedRules { get; init; }

    // -----------------------------------------------------------------------------------------
    // Diagnostic inputs
    // -----------------------------------------------------------------------------------------

    /// <summary>Where the [MethodOverloadGenerator] attribute was placed.</summary>
    public required AttributePlacement AttributePlacement { get; init; }

    /// <summary>
    /// The source location of the <c>[MethodOverloadGenerator]</c> attribute usage itself —
    /// on the class, method/constructor, or parameter, depending on <see cref="AttributePlacement"/>.
    /// Used as the reported location for every diagnostic in <see cref="Generation.DiagnosticsBuilder"/>,
    /// since the attribute usage is the one site common to all placement levels.
    /// </summary>
    public required Location AttributeLocation { get; init; }

    /// <summary>Whether the method has any <c>out</c> or <c>ref</c> parameters.</summary>
    public required bool HasOutOrRefParams { get; init; }

    /// <summary>
    /// The name of the parameter on which [MethodOverloadGenerator] was placed when
    /// <see cref="AttributePlacement"/> is <see cref="AttributePlacement.Parameter"/> and
    /// that parameter is not a delegate type; <see langword="null"/> otherwise.
    /// </summary>
    public required string? NonDelegateAttributedParamName { get; init; }

    /// <summary>
    /// Whether at least one rule that would otherwise apply to this method's delegate parameters
    /// has been explicitly disabled — used to emit a more specific "rules disabled" diagnostic
    /// instead of the generic "no overloads possible" diagnostic.
    /// </summary>
    public required bool AnyApplicableRuleDisabled { get; init; }

    // -----------------------------------------------------------------------------------------
    // Per-rule flags
    // -----------------------------------------------------------------------------------------

    /// <summary>Rule 1 — at least one attributed parameter has an async delegate.</summary>
    public required bool ApplyRule1 { get; init; }

    /// <summary>Rule 2 — at least one attributed parameter has a value-returning delegate.</summary>
    public required bool ApplyRule2 { get; init; }

    /// <summary>Rule 3 — at least one attributed parameter has a multi-input-parameter delegate.</summary>
    public required bool ApplyRule3 { get; init; }

    /// <summary>Rule 4 — the method is an extension method.</summary>
    public required bool ApplyRule4 { get; init; }

    /// <summary>Rule 5 — the method has two or more attributed delegate parameters.</summary>
    public required bool ApplyRule5 { get; init; }

    // -----------------------------------------------------------------------------------------
    // Rule contexts
    // Rules 1–3 are per-parameter (one entry per attributed delegate parameter that triggers
    // the rule); Rule 4 and 5 are per-method (at most one instance each).
    // -----------------------------------------------------------------------------------------

    /// <summary>One entry per attributed parameter eligible for a sync overload.</summary>
    public required IReadOnlyList<Rule1Context>? Rule1Contexts { get; init; }

    /// <summary>One entry per attributed parameter eligible for a fixed-value overload.</summary>
    public required IReadOnlyList<Rule2Context>? Rule2Contexts { get; init; }

    /// <summary>One entry per attributed parameter with two or more input parameters.</summary>
    public required IReadOnlyList<Rule3Context>? Rule3Contexts { get; init; }

    /// <summary>Extension-method receiver overload context; <see langword="null"/> when <see cref="ApplyRule4"/> is <see langword="false"/>.</summary>
    public required Rule4Context? Rule4Context { get; init; }

    /// <summary>Combinatorial overload context; <see langword="null"/> when <see cref="ApplyRule5"/> is <see langword="false"/>.</summary>
    public required Rule5Context? Rule5Context { get; init; }
}
