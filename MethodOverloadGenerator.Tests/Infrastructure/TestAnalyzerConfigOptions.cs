using Microsoft.CodeAnalysis.Diagnostics;

namespace MethodOverloadGenerator.Tests.Infrastructure;

/// <summary>
/// Dictionary-backed <see cref="AnalyzerConfigOptions"/> for tests.
/// Use <see cref="Empty"/> when no MSBuild overrides are needed.
/// </summary>
internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    public static readonly TestAnalyzerConfigOptions Empty = new([]);

    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptions(Dictionary<string, string> values) => _values = values;

    public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
        => _values.TryGetValue(key, out value);
}
