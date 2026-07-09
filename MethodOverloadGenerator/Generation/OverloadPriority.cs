using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Computes the <c>[OverloadResolutionPriority]</c> value for a generated overload, so that
/// IntelliSense and overload resolution always rank the original method first and, among the
/// generated overloads, the ones that keep the most of the original delegates' parameters next.
/// </summary>
/// <remarks>
/// The original method is never touched by the generator, so it keeps the implicit priority of
/// 0. Every generated overload gets a value strictly below zero, computed as
/// <c>-(1 + reduction)</c> where <c>reduction</c> is how many delegate input parameters this
/// overload gives up relative to the original, plus one extra point whenever it also turns an
/// originally-async delegate synchronous, summed across every substituted parameter:
/// <list type="bullet">
///   <item>Rule 1 (sync, same arity) — always async → sync, so <c>1</c>.</item>
///   <item>Rule 3, async-drop (arity N → k, stays async) — <c>N - k</c>.</item>
///   <item>Rule 3, fully-sync (arity N → k, also drops async) — <c>(N - k) + 1</c>: at the same
///         k, this ranks below the async-drop form, since it gives up strictly more.</item>
///   <item>Rule 4 (receiver only) — <c>0</c>: no delegate's arity or async-ness changes at all,
///         so this ranks above Rule 1 when the original delegates were async.</item>
///   <item>Rule 2 (fixed value) — <c>N + 1</c>, plus the async-lost point when the delegate was
///         async: worse than even a zero-input delegate, since the delegate abstraction itself
///         is gone.</item>
/// </list>
/// Combinatorial (Rule 5) overloads sum the reduction across every substituted parameter, so an
/// overload that changes two parameters ranks below one that changes only one of them the same way.
/// </remarks>
internal static class OverloadPriority
{
    /// <summary>Rule 1 always converts an async delegate to sync at its full original arity.</summary>
    public static int ForSync(DelegateInfo d) => LostAsyncPenalty(d, isNowAsync: false);

    /// <summary>Rule 3: arity N → k, optionally also converting to sync.</summary>
    public static int ForReducedArity(DelegateInfo d, int k, bool preserveAsync)
        => (d.InputTypes.Count - k) + LostAsyncPenalty(d, isNowAsync: preserveAsync);

    /// <summary>Rule 2: the delegate is replaced with a plain value, which is never async.</summary>
    public static int ForFixedValue(DelegateInfo d)
        => d.InputTypes.Count + 1 + LostAsyncPenalty(d, isNowAsync: false);

    // One extra point of reduction whenever a substitution turns an originally-async delegate
    // synchronous — a delegate that was never async to begin with has nothing to lose here.
    private static int LostAsyncPenalty(DelegateInfo d, bool isNowAsync)
        => d.IsAsync && !isNowAsync ? 1 : 0;

    public static int ToPriority(int totalReduction) => -(1 + totalReduction);

    /// <summary>The <c>[OverloadResolutionPriority(...)]</c> line to prepend before the method's modifiers.</summary>
    public static string Attribute(int totalReduction)
        => $"    [OverloadResolutionPriority({ToPriority(totalReduction)})]\n";
}
