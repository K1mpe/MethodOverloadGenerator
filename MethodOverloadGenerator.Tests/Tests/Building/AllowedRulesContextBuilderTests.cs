namespace MethodOverloadGenerator.Tests.Building;

/// <summary>
/// MSBuild property key convention expected by <see cref="AllowedRulesContextBuilder"/>.
/// Values are <c>"true"</c> / <c>"false"</c>.
/// </summary>
file static class MsBuildKeys
{
    public const string SyncOverloads              = "build_property.MethodOverloadGenerator_SyncOverloads";
    public const string ValueOverloads             = "build_property.MethodOverloadGenerator_ValueOverloads";
    public const string TrailingParameterOverloads = "build_property.MethodOverloadGenerator_TrailingParameterOverloads";
    public const string TaskReceiverOverloads      = "build_property.MethodOverloadGenerator_TaskReceiverOverloads";
    public const string CombinatorialOverloads     = "build_property.MethodOverloadGenerator_CombinatorialOverloads";
}

public class AllowedRulesContextBuilderTests
{
    private const string MethodWithDefaultAttribute = """
        using MethodOverloadGenerator;
        public class C
        {
            [MethodOverloadGeneratorAttribute]
            public void M() {}
        }
        """;

    private readonly AllowedRulesContextBuilder _sut = new();

    private static Microsoft.CodeAnalysis.AttributeData DefaultAttribute()
        => RoslynTestHelper.GetMethodOverloadAttribute(MethodWithDefaultAttribute, "C", "M");

    private static Microsoft.CodeAnalysis.AttributeData AttributeWith(string args)
    {
        var source = $$"""
            using MethodOverloadGenerator;
            public class C
            {
                [MethodOverloadGeneratorAttribute({{args}})]
                public void M() {}
            }
            """;
        return RoslynTestHelper.GetMethodOverloadAttribute(source, "C", "M");
    }

    // -----------------------------------------------------------------------------------------
    // Default: all rules allowed when nothing is configured
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllRulesAllowed_WhenAttributeAndMsBuildAreAllDefault()
    {
        var result = _sut.Build(DefaultAttribute(), TestAnalyzerConfigOptions.Empty);

        Assert.True(result.AllowRule1);
        Assert.True(result.AllowRule2);
        Assert.True(result.AllowRule3);
        Assert.True(result.AllowRule4);
        Assert.True(result.AllowRule5);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute Disable overrides everything
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllowRule1_IsFalse_WhenAttributeDisablesRule1()
    {
        var result = _sut.Build(
            AttributeWith("syncOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.False(result.AllowRule1);
    }

    [Fact]
    public void AllowRule2_IsFalse_WhenAttributeDisablesRule2()
    {
        var result = _sut.Build(
            AttributeWith("valueOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.False(result.AllowRule2);
    }

    [Fact]
    public void AllowRule3_IsFalse_WhenAttributeDisablesRule3()
    {
        var result = _sut.Build(
            AttributeWith("trailingParameterOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.False(result.AllowRule3);
    }

    [Fact]
    public void AllowRule4_IsFalse_WhenAttributeDisablesRule4()
    {
        var result = _sut.Build(
            AttributeWith("taskReceiverOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.False(result.AllowRule4);
    }

    [Fact]
    public void AllowRule5_IsFalse_WhenAttributeDisablesRule5()
    {
        var result = _sut.Build(
            AttributeWith("combinatorialOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.False(result.AllowRule5);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute Enable overrides MSBuild false
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllowRule1_IsTrue_WhenAttributeEnables_EvenIfMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "false" });

        var result = _sut.Build(AttributeWith("syncOverloads: RuleOverride.Enable"), options);

        Assert.True(result.AllowRule1);
    }

    [Fact]
    public void AllowRule2_IsTrue_WhenAttributeEnables_EvenIfMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.ValueOverloads] = "false" });

        var result = _sut.Build(AttributeWith("valueOverloads: RuleOverride.Enable"), options);

        Assert.True(result.AllowRule2);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute Default falls through to MSBuild
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllowRule1_IsFalse_WhenAttributeIsDefault_AndMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "false" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.False(result.AllowRule1);
    }

    [Fact]
    public void AllowRule2_IsFalse_WhenAttributeIsDefault_AndMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.ValueOverloads] = "false" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.False(result.AllowRule2);
    }

    [Fact]
    public void AllowRule3_IsFalse_WhenAttributeIsDefault_AndMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TrailingParameterOverloads] = "false" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.False(result.AllowRule3);
    }

    [Fact]
    public void AllowRule4_IsFalse_WhenAttributeIsDefault_AndMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.TaskReceiverOverloads] = "false" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.False(result.AllowRule4);
    }

    [Fact]
    public void AllowRule5_IsFalse_WhenAttributeIsDefault_AndMsBuildDisables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.CombinatorialOverloads] = "false" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.False(result.AllowRule5);
    }

    [Fact]
    public void AllowRule1_IsTrue_WhenAttributeIsDefault_AndMsBuildEnables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "true" });

        var result = _sut.Build(DefaultAttribute(), options);

        Assert.True(result.AllowRule1);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute Disable wins over MSBuild true
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllowRule1_IsFalse_WhenAttributeDisables_EvenIfMsBuildEnables()
    {
        var options = new TestAnalyzerConfigOptions(new() { [MsBuildKeys.SyncOverloads] = "true" });

        var result = _sut.Build(AttributeWith("syncOverloads: RuleOverride.Disable"), options);

        Assert.False(result.AllowRule1);
    }

    // -----------------------------------------------------------------------------------------
    // Other rules remain unaffected by changes to a single rule
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void OtherRulesStayAllowed_WhenOnlyRule1IsDisabled()
    {
        var result = _sut.Build(
            AttributeWith("syncOverloads: RuleOverride.Disable"),
            TestAnalyzerConfigOptions.Empty);

        Assert.True(result.AllowRule2);
        Assert.True(result.AllowRule3);
        Assert.True(result.AllowRule4);
        Assert.True(result.AllowRule5);
    }
}
