using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building.Rules;

/// <summary>
/// Builds the per-parameter <see cref="Rule1Context"/> list for the sync-overload rule.
/// Returns one entry for each <see cref="DelegateInfo"/> whose delegate is async and whose
/// rule is allowed by <paramref name="allowedRules"/>.
/// </summary>
internal sealed class Rule1ContextsBuilder
{
    public IReadOnlyList<Rule1Context> Build(
        IReadOnlyList<DelegateInfo> delegateParameters,
        AllowedRulesContext allowedRules)
    {
        if (!allowedRules.AllowRule1)
            return [];

        return delegateParameters
            .Where(d => d.IsAsync)
            .Select(d => new Rule1Context { Delegate = d })
            .ToList();
    }
}
