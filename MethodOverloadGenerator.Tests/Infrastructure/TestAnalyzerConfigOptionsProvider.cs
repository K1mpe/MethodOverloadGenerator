using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MethodOverloadGenerator.Tests.Infrastructure;

/// <summary>
/// Minimal <see cref="AnalyzerConfigOptionsProvider"/> for tests — the generator only ever reads
/// <see cref="GlobalOptions"/> (MSBuild properties are project-wide, not per-file), so the
/// per-tree/per-text overloads just return the same options.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public static readonly TestAnalyzerConfigOptionsProvider Empty = new(TestAnalyzerConfigOptions.Empty);

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions globalOptions) => GlobalOptions = globalOptions;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> msBuildProperties)
        : this(new TestAnalyzerConfigOptions(msBuildProperties)) { }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
}
