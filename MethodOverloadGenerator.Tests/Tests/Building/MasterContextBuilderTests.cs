using Microsoft.CodeAnalysis;

namespace MethodOverloadGenerator.Tests.Building;

file static class MsBuildKeys
{
    public const string SyncOverloads              = "build_property.MethodOverloadGenerator_SyncOverloads";
    public const string ValueOverloads             = "build_property.MethodOverloadGenerator_ValueOverloads";
    public const string TrailingParameterOverloads = "build_property.MethodOverloadGenerator_TrailingParameterOverloads";
    public const string TaskReceiverOverloads      = "build_property.MethodOverloadGenerator_TaskReceiverOverloads";
    public const string CombinatorialOverloads     = "build_property.MethodOverloadGenerator_CombinatorialOverloads";
}

public class MasterContextBuilderTests
{
    private static MasterContextBuilder CreateBuilder() => new(
        new DeclarationContextBuilder(),
        new AllowedRulesContextBuilder(),
        new DelegateParametersBuilder(),
        new Rule1ContextsBuilder(),
        new Rule2ContextsBuilder(),
        new Rule3ContextsBuilder(),
        new Rule4ContextBuilder(),
        new Rule5ContextBuilder());

    private static (IMethodSymbol Method, AttributeData Attribute) GetMethodAndAttribute(string source)
    {
        var method    = RoslynTestHelper.GetMethod(source, "C", "M");
        var attribute = RoslynTestHelper.GetMethodOverloadAttribute(source, "C", "M");
        return (method, attribute);
    }

    private const string SimpleSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Action f) {}
        }
        """;

    private const string AsyncDelegateSource = """
        using System.Threading.Tasks;
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Func<Task> f) {}
        }
        """;

    private const string ValueDelegateSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Func<int> f) {}
        }
        """;

    private const string MultiInputDelegateSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Action<int, string> f) {}
        }
        """;

    private const string ExtensionMethodSource = """
        using System.Threading.Tasks;
        using MethodOverloadGenerator;
        public static partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public static Task M(this string s) => Task.CompletedTask;
        }
        """;

    private const string TwoDelegatesSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Action f, System.Action g) {}
        }
        """;

    private const string OutParamSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Action f, out int x) { x = 0; }
        }
        """;

    private const string RefParamSource = """
        using MethodOverloadGenerator;
        public partial class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M(System.Action f, ref int x) {}
        }
        """;

    // -----------------------------------------------------------------------------------------
    // AttributePlacement — stored as-is from the parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AttributePlacement_Class_IsStoredCorrectly()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Class);

        Assert.Equal(AttributePlacement.Class, result.AttributePlacement);
    }

    [Fact]
    public void AttributePlacement_Method_IsStoredCorrectly()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.Equal(AttributePlacement.Method, result.AttributePlacement);
    }

    [Fact]
    public void AttributePlacement_Parameter_IsStoredCorrectly()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Parameter);

        Assert.Equal(AttributePlacement.Parameter, result.AttributePlacement);
    }

    // -----------------------------------------------------------------------------------------
    // NonDelegateAttributedParamName — stored as-is from the parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonDelegateAttributedParamName_IsNull_ByDefault()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.Null(result.NonDelegateAttributedParamName);
    }

    [Fact]
    public void NonDelegateAttributedParamName_IsStored_WhenProvided()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Parameter, nonDelegateAttributedParamName: "capacity");

        Assert.Equal("capacity", result.NonDelegateAttributedParamName);
    }

    // -----------------------------------------------------------------------------------------
    // HasOutOrRefParams
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void HasOutOrRefParams_IsFalse_WhenNoOutOrRefParams()
    {
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.False(result.HasOutOrRefParams);
    }

    [Fact]
    public void HasOutOrRefParams_IsTrue_WhenMethodHasOutParam()
    {
        var (method, attr) = GetMethodAndAttribute(OutParamSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.True(result.HasOutOrRefParams);
    }

    [Fact]
    public void HasOutOrRefParams_IsTrue_WhenMethodHasRefParam()
    {
        var (method, attr) = GetMethodAndAttribute(RefParamSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.True(result.HasOutOrRefParams);
    }

    // -----------------------------------------------------------------------------------------
    // AnyApplicableRuleDisabled — true when a rule that would apply has been disabled
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AnyApplicableRuleDisabled_IsFalse_WhenAllRulesAreEnabled()
    {
        var (method, attr) = GetMethodAndAttribute(AsyncDelegateSource);

        var result = CreateBuilder().Build(method, attr, TestAnalyzerConfigOptions.Empty,
            AttributePlacement.Method);

        Assert.False(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsTrue_WhenRule1DisabledAndAsyncDelegatePresent()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(AsyncDelegateSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.True(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsFalse_WhenRule1DisabledButNoDelegateIsAsync()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(ValueDelegateSource); // Func<int> — not async

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.False(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsTrue_WhenRule2DisabledAndValueDelegatePresent()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.ValueOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(ValueDelegateSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.True(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsTrue_WhenRule3DisabledAndMultiInputDelegatePresent()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TrailingParameterOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(MultiInputDelegateSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.True(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsFalse_WhenRule3DisabledButDelegateHasFewerThanTwoInputParams()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TrailingParameterOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(SimpleSource); // Action — no input params

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.False(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsTrue_WhenRule4DisabledAndMethodIsExtension()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TaskReceiverOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(ExtensionMethodSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.True(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsFalse_WhenRule4DisabledButMethodIsNotExtension()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TaskReceiverOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(SimpleSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.False(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsTrue_WhenRule5DisabledAndTwoDelegateParamsPresent()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.CombinatorialOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(TwoDelegatesSource);

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.True(result.AnyApplicableRuleDisabled);
    }

    [Fact]
    public void AnyApplicableRuleDisabled_IsFalse_WhenRule5DisabledButOnlyOneDelegateParam()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.CombinatorialOverloads] = "false" });
        var (method, attr) = GetMethodAndAttribute(SimpleSource); // single Action param

        var result = CreateBuilder().Build(method, attr, options, AttributePlacement.Method);

        Assert.False(result.AnyApplicableRuleDisabled);
    }
}
