namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Builds the type-parameter list and <c>where</c> clauses for a generated overload's method
/// header: the original method's own type parameters/constraints (which every overload must
/// redeclare, since it forwards to the original method by name) plus, for Rule 4/5 receiver
/// overloads, an extra type parameter synthesized for <c>Task&lt;T&gt;</c> covariance — unless
/// that synthesized parameter reuses one of the originals, in which case it isn't duplicated.
/// </summary>
internal static class GenericSignatureHelper
{
    public static string BuildTypeParameterList(IReadOnlyList<string> original, string? extra)
    {
        var names = extra is null || original.Contains(extra)
            ? original
            : [.. original, extra];

        return names.Count == 0 ? "" : $"<{string.Join(", ", names)}>";
    }

    public static string BuildWhereClauses(IReadOnlyList<string> original, string? extra)
    {
        var clauses = extra is null ? original : [.. original, extra];
        return string.Concat(clauses.Select(c => $"\n        {c}"));
    }
}
