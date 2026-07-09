namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Context for Rule 3 — Multi-parameter delegate → trailing-parameter overload.
/// One instance per (attributed delegate parameter, target arity k, sync/async form) triple
/// where the delegate has at least one input parameter.
/// </summary>
/// <remarks>
/// Non-async delegates produce exactly one context per k: the plain-reduced form (keeping the
/// sync return type at k inputs) — there is no async form to offer. Async delegates produce
/// <em>two</em> contexts per k: <see cref="PreserveAsync"/> <see langword="true"/> for the
/// async-drop form (keeping the async return type at k inputs), and <see langword="false"/> for
/// the fully-sync form (dropping both the trailing inputs and the async wrapper). Both are
/// independently useful — a caller may still want to pass an async delegate with fewer
/// parameters — and offering both means Rule 5 can cross the fully-sync form with another
/// parameter's own substitution (e.g. <c>Eat(Func&lt;int, bool, ICarnivore&gt; make, Func&lt;int&gt; fetchPrey)</c>).
/// </remarks>
internal sealed record Rule3Context
{
    public required DelegateInfo Delegate { get; init; }

    /// <summary>
    /// The number of input parameters the reduced delegate accepts.
    /// Ranges from N−1 down to 0 where N = <see cref="Delegate"/>.InputTypes.Count.
    /// </summary>
    public required int TargetInputCount { get; init; }

    /// <summary>
    /// <see langword="true"/> to keep the delegate async at the reduced arity (async-drop form);
    /// <see langword="false"/> to also convert it to a synchronous return (fully-sync form).
    /// Always <see langword="false"/> when <see cref="Delegate"/> is not async to begin with —
    /// there is nothing to preserve.
    /// </summary>
    public required bool PreserveAsync { get; init; }
}
