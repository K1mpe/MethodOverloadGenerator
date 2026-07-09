namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Context for Rule 1 — Async delegate → sync overload.
/// One instance per attributed delegate parameter that has an async return type
/// (<c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c>, or <c>ValueTask&lt;T&gt;</c>).
/// </summary>
/// <remarks>
/// From <see cref="Delegate"/> the emitter derives:
/// <list type="bullet">
///   <item>The sync parameter type (<c>Func&lt;T&gt;</c> or <c>Action</c> / <c>Action&lt;…&gt;</c>)</item>
///   <item>The wrap expression (<c>Task.FromResult</c>, <c>new ValueTask&lt;T&gt;</c>, <c>Task.CompletedTask</c>, …)</item>
/// </list>
/// </remarks>
internal sealed record Rule1Context
{
    public required DelegateInfo Delegate { get; init; }
}
