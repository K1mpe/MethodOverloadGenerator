namespace MethodOverloadGenerator.Tests.Building.Rules;

public class Rule5ContextBuilderTests
{
    private readonly Rule5ContextBuilder _sut = new();

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true, AllowRule4 = true, AllowRule5 = true,
    };

    private static AllowedRulesContext Rule5Disabled => AllAllowed with { AllowRule5 = false };

    private static DelegateInfo Delegate(string name) => new()
    {
        ParameterName = name,
        InputTypes    = [],
        ReturnType    = null,
        IsAsync       = false,
        IsValueTask   = false,
    };

    // -----------------------------------------------------------------------------------------
    // Eligibility guard (requires 2+ delegate parameters)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsNull_WhenDelegateListIsEmpty()
    {
        Assert.Null(_sut.Build([], AllAllowed));
    }

    [Fact]
    public void ReturnsNull_WhenOnlyOneDelegateParameter()
    {
        var context = new Rule5ContextBuilder();
        Assert.Null(_sut.Build([Delegate("a")], AllAllowed));
    }

    [Fact]
    public void ReturnsNull_WhenRule5IsNotAllowed()
    {
        Assert.Null(_sut.Build([Delegate("a"), Delegate("b")], Rule5Disabled));
    }

    // -----------------------------------------------------------------------------------------
    // Eligibility guard with a receiver rule (extension methods) — one delegate parameter is
    // enough, since it can still be combined with the Task<T>/ValueTask<T> receiver dimension
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsNull_WhenDelegateListIsEmpty_EvenWithReceiverRule()
    {
        Assert.Null(_sut.Build([], AllAllowed, hasReceiverRule: true));
    }

    [Fact]
    public void ReturnsContext_WhenOnlyOneDelegateParameter_AndHasReceiverRule()
    {
        Assert.NotNull(_sut.Build([Delegate("a")], AllAllowed, hasReceiverRule: true));
    }

    [Fact]
    public void ReturnsNull_WhenRule5IsNotAllowed_EvenWithReceiverRule()
    {
        Assert.Null(_sut.Build([Delegate("a")], Rule5Disabled, hasReceiverRule: true));
    }

    // -----------------------------------------------------------------------------------------
    // Inclusion
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsContext_WhenTwoDelegateParameters_AndAllowed()
    {
        Assert.NotNull(_sut.Build([Delegate("a"), Delegate("b")], AllAllowed));
    }

    [Fact]
    public void ReturnsContext_WhenThreeDelegateParameters_AndAllowed()
    {
        Assert.NotNull(_sut.Build([Delegate("a"), Delegate("b"), Delegate("c")], AllAllowed));
    }

    // -----------------------------------------------------------------------------------------
    // AttributedParameters content
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AttributedParameters_ContainsBothDelegates_ForTwoParameterMethod()
    {
        var a = Delegate("a");
        var b = Delegate("b");

        var result = _sut.Build([a, b], AllAllowed)!;

        Assert.Equal(2, result.AttributedParameters.Count);
        Assert.Same(a, result.AttributedParameters[0]);
        Assert.Same(b, result.AttributedParameters[1]);
    }

    [Fact]
    public void AttributedParameters_ContainsAllDelegates_ForThreeParameterMethod()
    {
        var a = Delegate("a");
        var b = Delegate("b");
        var c = Delegate("c");

        var result = _sut.Build([a, b, c], AllAllowed)!;

        Assert.Equal(3, result.AttributedParameters.Count);
        Assert.Same(a, result.AttributedParameters[0]);
        Assert.Same(b, result.AttributedParameters[1]);
        Assert.Same(c, result.AttributedParameters[2]);
    }

    [Fact]
    public void AttributedParameters_PreservesDeclarationOrder()
    {
        var first  = Delegate("first");
        var second = Delegate("second");

        var result = _sut.Build([first, second], AllAllowed)!;

        Assert.Equal("first",  result.AttributedParameters[0].ParameterName);
        Assert.Equal("second", result.AttributedParameters[1].ParameterName);
    }
}
