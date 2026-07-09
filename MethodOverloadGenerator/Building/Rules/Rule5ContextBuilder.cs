using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building.Rules;

/// <summary>
/// Builds the <see cref="Rule5Context"/> for the combinatorial multi-parameter rule.
/// Returns <see langword="null"/> when there are too few delegate parameters to combine,
/// or when the rule is not allowed by <paramref name="allowedRules"/>.
/// </summary>
internal sealed class Rule5ContextBuilder
{
    /// <param name="delegateParameters">Delegate parameters eligible for combinatorial substitution.</param>
    /// <param name="allowedRules">Per-rule enablement for this method.</param>
    /// <param name="hasReceiverRule">
    /// <see langword="true"/> when the method is also eligible for Rule 4 (extension-method
    /// <c>Task&lt;T&gt;</c>/<c>ValueTask&lt;T&gt;</c> receiver overloads). Normally at least two
    /// delegate parameters are required — a lone parameter's own substitutions are already
    /// covered by Rules 1–3 — but when a receiver dimension exists, a single parameter is still
    /// combinatorially interesting crossed with that receiver (e.g. <c>Eat&lt;T&gt;(this
    /// Task&lt;T&gt;, Func&lt;int&gt; fetchPrey)</c>), so one parameter is enough.
    /// </param>
    public Rule5Context? Build(
        IReadOnlyList<DelegateInfo> delegateParameters,
        AllowedRulesContext allowedRules,
        bool hasReceiverRule = false)
    {
        var minimumParameterCount = hasReceiverRule ? 1 : 2;

        if (!allowedRules.AllowRule5 || delegateParameters.Count < minimumParameterCount)
            return null;

        return new Rule5Context { AttributedParameters = delegateParameters };
    }
}
