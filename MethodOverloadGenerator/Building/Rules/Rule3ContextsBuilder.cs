using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building.Rules;

/// <summary>
/// Builds the per-parameter <see cref="Rule3Context"/> list for the trailing-parameter-variant rule.
/// Returns entries for each <see cref="DelegateInfo"/> whose delegate has one or more input
/// parameters and whose rule is allowed by <paramref name="allowedRules"/> — one entry per k for
/// non-async delegates, two per k (async-drop and fully-sync) for async delegates.
/// </summary>
internal sealed class Rule3ContextsBuilder
{
    public IReadOnlyList<Rule3Context> Build(
        IReadOnlyList<DelegateInfo> delegateParameters,
        AllowedRulesContext allowedRules)
    {
        if (!allowedRules.AllowRule3)
            return [];

        return delegateParameters
            .Where(d => d.InputTypes.Count >= 1)
            .SelectMany(d => Enumerable.Range(0, d.InputTypes.Count).Reverse().SelectMany(k => ContextsForArity(d, k)))
            .ToList();
    }

    private static IEnumerable<Rule3Context> ContextsForArity(DelegateInfo d, int k)
    {
        if (d.IsAsync)
            yield return new Rule3Context { Delegate = d, TargetInputCount = k, PreserveAsync = true };

        yield return new Rule3Context { Delegate = d, TargetInputCount = k, PreserveAsync = false };
    }
}
