namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Context for Rule 2 — Value-returning delegate → fixed-value overload.
/// One instance per attributed delegate parameter whose <see cref="DelegateInfo.ReturnType"/>
/// is non-null (i.e. the delegate returns a usable value).
/// </summary>
/// <remarks>
/// From <see cref="Delegate"/> the emitter derives:
/// <list type="bullet">
///   <item>The value parameter type (equals <see cref="DelegateInfo.ReturnType"/>)</item>
///   <item>The wrap expression (<c>Task.FromResult(value)</c>, <c>new ValueTask&lt;T&gt;(value)</c>,
///         or <c>() =&gt; value</c> for sync delegates)</item>
/// </list>
/// </remarks>
internal sealed record Rule2Context
{
    public required DelegateInfo Delegate { get; init; }
}
