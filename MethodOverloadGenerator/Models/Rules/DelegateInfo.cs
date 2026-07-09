namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Carries the structural information extracted from a single delegate-typed parameter.
/// Reused by Rule1Context, Rule2Context, Rule3Context, and Rule5Context.
/// </summary>
internal sealed record DelegateInfo
{
    /// <summary>
    /// Name of the original delegate-typed parameter on the method being overloaded
    /// (e.g. <c>fetchDog</c> in <c>Func&lt;Task&lt;Dog&gt;&gt; fetchDog</c>).
    /// The generated overloads reuse this name for their own replacement parameter so that
    /// the caller-facing API stays consistent — e.g. <c>Admit(Dog fetchDog)</c> rather than
    /// an anonymous or renamed parameter.
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// Ordered list of the delegate's input parameter types, rendered as source strings
    /// (e.g. <c>["int", "double"]</c> for <c>Func&lt;int, double, Task&lt;string&gt;&gt;</c>).
    /// Empty for zero-input delegates such as <c>Func&lt;Task&lt;int&gt;&gt;</c> or <c>Action</c>.
    /// </summary>
    public required IReadOnlyList<string> InputTypes { get; init; }

    /// <summary>
    /// The unwrapped return type of the delegate, rendered as a source string.
    /// <list type="bullet">
    ///   <item><c>"int"</c> for <c>Func&lt;Task&lt;int&gt;&gt;</c> or <c>Func&lt;int&gt;</c></item>
    ///   <item><c>null</c> for void-returning delegates (<c>Action</c>, <c>Func&lt;Task&gt;</c>,
    ///         <c>Func&lt;ValueTask&gt;</c>)</item>
    /// </list>
    /// Rule 2 applies when this is non-null. Rule 1 applies when <see cref="IsAsync"/> is true.
    /// </summary>
    public required string? ReturnType { get; init; }

    /// <summary>
    /// <see langword="true"/> when the delegate's return type is <c>Task</c>, <c>Task&lt;T&gt;</c>,
    /// <c>ValueTask</c>, or <c>ValueTask&lt;T&gt;</c>.
    /// </summary>
    public required bool IsAsync { get; init; }

    /// <summary>
    /// <see langword="true"/> when <see cref="IsAsync"/> is <see langword="true"/> and the
    /// async type is <c>ValueTask</c> / <c>ValueTask&lt;T&gt;</c> rather than <c>Task</c>.
    /// </summary>
    public required bool IsValueTask { get; init; }
}
