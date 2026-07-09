namespace MethodOverloadGenerator.Models;

/// <summary>
/// A single parameter of the method being overloaded, captured as minimally-qualified
/// source strings so emitters can reconstruct the method signature without a Roslyn dependency.
/// </summary>
internal sealed record MethodParameter
{
    /// <summary>Minimally-qualified type (e.g. <c>"Func&lt;Task&lt;Dog&gt;&gt;"</c>, <c>"int"</c>).</summary>
    public required string Type { get; init; }

    /// <summary>Parameter name as declared in source (e.g. <c>"fetchDog"</c>).</summary>
    public required string Name { get; init; }
}
