using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building.Rules;

/// <summary>
/// Builds the per-parameter <see cref="Rule2Context"/> list for the fixed-value-overload rule.
/// Returns one entry for each <see cref="DelegateInfo"/> whose delegate has a non-void return
/// type and whose rule is allowed by <paramref name="allowedRules"/>.
/// </summary>
internal sealed class Rule2ContextsBuilder
{
    public IReadOnlyList<Rule2Context> Build(
        IReadOnlyList<DelegateInfo> delegateParameters,
        AllowedRulesContext allowedRules)
    {
        if (!allowedRules.AllowRule2)
            return [];

        return delegateParameters
            .Where(d => d.ReturnType is not null)
            .Select(d => new Rule2Context { Delegate = d })
            .ToList();
    }
}
