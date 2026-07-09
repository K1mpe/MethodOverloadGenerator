namespace MethodOverloadGenerator.Models.Rules;

/// <summary>
/// Context for Rule 4 — Extension method → <c>Task&lt;T&gt;</c> / <c>ValueTask&lt;T&gt;</c> receiver overloads.
/// One instance per extension method (there is at most one <c>this</c> parameter per method).
/// </summary>
/// <remarks>
/// The emitter generates two overloads: one where the <c>this</c> parameter is
/// <c>Task&lt;<see cref="ThisParameterType"/>&gt;</c> and one where it is
/// <c>ValueTask&lt;<see cref="ThisParameterType"/>&gt;</c>.
/// Both overloads are always <c>async</c> because they must <c>await</c> the input.
/// </remarks>
internal sealed record Rule4Context
{
    /// <summary>Source-rendered type of the <c>this</c> parameter (e.g. <c>"ICarnivore"</c>).</summary>
    public required string ThisParameterType { get; init; }

    /// <summary>Name of the <c>this</c> parameter as it appears in source (e.g. <c>"animal"</c>).</summary>
    public required string ThisParameterName { get; init; }

    /// <summary>
    /// Return type of the original method, rendered as a source string
    /// (e.g. <c>"IPrey"</c>, <c>"Task&lt;IPrey&gt;"</c>).
    /// <see langword="null"/> when the method returns <c>void</c>.
    /// </summary>
    public required string? MethodReturnType { get; init; }

    /// <summary>
    /// <see langword="true"/> when the original method itself is async
    /// (i.e. its return type is <c>Task</c> or <c>Task&lt;T&gt;</c> / <c>ValueTask</c> etc.).
    /// Determines the exact body expression emitted for each generated overload.
    /// </summary>
    public required bool IsAsyncMethod { get; init; }

    /// <summary>
    /// Name of the type parameter to use in place of <see cref="ThisParameterType"/> when
    /// building the <c>Task&lt;…&gt;</c> / <c>ValueTask&lt;…&gt;</c> receiver type, or
    /// <see langword="null"/> when <see cref="ThisParameterType"/> must be used as-is.
    /// </summary>
    /// <remarks>
    /// <c>Task&lt;T&gt;</c> is not covariant, so hard-coding the receiver as
    /// <c>Task&lt;<see cref="ThisParameterType"/>&gt;</c> would reject a caller's
    /// <c>Task&lt;TDerived&gt;</c> even though <c>TDerived</c> is assignable to
    /// <see cref="ThisParameterType"/> (e.g. a <c>Task&lt;Dog&gt;</c> could not be passed to an
    /// overload expecting <c>Task&lt;ICarnivore&gt;</c>). To keep the overload usable for any
    /// subtype, the generator introduces a fresh type parameter constrained to
    /// <see cref="ThisParameterType"/> (see <see cref="RequiresGenericConstraint"/>) whenever the
    /// original type could have subtypes (an interface or a non-sealed class). When
    /// <see cref="ThisParameterType"/> is already a type parameter of the original method, that
    /// same name is reused here instead of synthesizing a new one. When
    /// <see cref="ThisParameterType"/> cannot have subtypes (e.g. <c>string</c>, a sealed class,
    /// or a struct), no type parameter is needed at all and this is <see langword="null"/>.
    /// </remarks>
    public required string? GenericParameterName { get; init; }

    /// <summary>
    /// <see langword="true"/> when <see cref="GenericParameterName"/> is a newly synthesized type
    /// parameter that must be declared with a <c>where {GenericParameterName} : {ThisParameterType}</c>
    /// constraint. <see langword="false"/> when <see cref="GenericParameterName"/> is
    /// <see langword="null"/>, or when it reuses a type parameter that already exists on the
    /// original method (which needs no additional constraint).
    /// </summary>
    public required bool RequiresGenericConstraint { get; init; }
}
