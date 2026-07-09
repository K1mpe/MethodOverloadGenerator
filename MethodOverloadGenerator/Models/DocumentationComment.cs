namespace MethodOverloadGenerator.Models;

/// <summary>
/// The <c>&lt;summary&gt;</c> and <c>&lt;remarks&gt;</c> XML doc content extracted from the
/// original method that carries <c>[MethodOverloadGenerator]</c>, so generated overloads can
/// reproduce the summary and extend the remarks instead of shipping undocumented.
/// </summary>
internal sealed record DocumentationComment
{
    /// <summary>
    /// Inner XML of the original method's <c>&lt;summary&gt;</c> element, or
    /// <see langword="null"/> when the method has no doc comment or no <c>&lt;summary&gt;</c>.
    /// </summary>
    public required string? Summary { get; init; }

    /// <summary>
    /// Inner XML of the original method's <c>&lt;remarks&gt;</c> element, or
    /// <see langword="null"/> when the method has no <c>&lt;remarks&gt;</c>.
    /// Generated overloads append their own note after this content rather than replacing it.
    /// </summary>
    public required string? Remarks { get; init; }

    /// <summary>Documentation instance for methods with no doc comment at all.</summary>
    public static readonly DocumentationComment None = new() { Summary = null, Remarks = null };
}
