using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Builds the XML doc comment prepended to each generated overload: the original method's
/// <c>&lt;summary&gt;</c> reproduced unchanged, followed by a <c>&lt;remarks&gt;</c> section
/// that keeps the original remarks (if any) and appends a rule-specific note describing how
/// this particular overload differs from the method it forwards to.
/// </summary>
internal static class DocCommentBuilder
{
    /// <param name="documentation">The original method's extracted summary/remarks.</param>
    /// <param name="generatedRemark">
    /// Rule-specific note appended to the <c>&lt;remarks&gt;</c> section, e.g. what got
    /// substituted and any behavioral difference the caller should be aware of.
    /// </param>
    /// <returns>Doc comment lines, each already prefixed with the method's indentation and <c>///</c>.</returns>
    public static string Build(DocumentationComment documentation, string generatedRemark)
    {
        var lines = new List<string>();

        if (documentation.Summary is { } summary)
        {
            lines.Add("<summary>");
            lines.AddRange(SplitLines(summary));
            lines.Add("</summary>");
        }

        lines.Add("<remarks>");
        if (documentation.Remarks is { } remarks)
            lines.AddRange(SplitLines(remarks));
        lines.Add(generatedRemark);
        lines.Add("</remarks>");

        return string.Join("\n", lines.Select(l => "    /// " + l)) + "\n";
    }

    private static IEnumerable<string> SplitLines(string text)
        => text.Replace("\r\n", "\n").Split('\n');
}
