namespace MethodOverloadGenerator.Tests.Building.Rules;

public class Rule2ContextsBuilderTests
{
    private readonly Rule2ContextsBuilder _sut = new();

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true, AllowRule4 = true, AllowRule5 = true,
    };

    private static AllowedRulesContext Rule2Disabled => AllAllowed with { AllowRule2 = false };

    private static DelegateInfo ValueReturning(string name = "f", string returnType = "int") => new()
    {
        ParameterName = name,
        InputTypes    = [],
        ReturnType    = returnType,
        IsAsync       = false,
        IsValueTask   = false,
    };

    private static DelegateInfo VoidDelegate(string name = "f") => new()
    {
        ParameterName = name,
        InputTypes    = [],
        ReturnType    = null,
        IsAsync       = false,
        IsValueTask   = false,
    };

    // -----------------------------------------------------------------------------------------
    // Eligibility guard
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsEmpty_WhenDelegateListIsEmpty()
    {
        Assert.Empty(_sut.Build([], AllAllowed));
    }

    [Fact]
    public void ReturnsEmpty_WhenDelegateIsVoid()
    {
        Assert.Empty(_sut.Build([VoidDelegate()], AllAllowed));
    }

    [Fact]
    public void ReturnsEmpty_WhenRule2IsNotAllowed()
    {
        Assert.Empty(_sut.Build([ValueReturning()], Rule2Disabled));
    }

    // -----------------------------------------------------------------------------------------
    // Inclusion
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsOneEntry_ForValueReturningDelegate_WhenAllowed()
    {
        Assert.Single(_sut.Build([ValueReturning()], AllAllowed));
    }

    [Fact]
    public void ReturnsOnlyValueReturningDelegates_WhenMixed()
    {
        var result = _sut.Build([VoidDelegate("void"), ValueReturning("val")], AllAllowed);

        Assert.Single(result);
        Assert.Equal("val", result[0].Delegate.ParameterName);
    }

    [Fact]
    public void ReturnsMultipleEntries_ForMultipleValueReturningDelegates()
    {
        Assert.Equal(2, _sut.Build([ValueReturning("a"), ValueReturning("b")], AllAllowed).Count);
    }

    // -----------------------------------------------------------------------------------------
    // Context content
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ResultEntry_ContainsOriginalDelegateInfo()
    {
        var delegate1 = ValueReturning("fetchCount");
        var result = _sut.Build([delegate1], AllAllowed);

        Assert.Same(delegate1, result[0].Delegate);
    }

    [Fact]
    public void ResultEntries_PreserveDeclarationOrder()
    {
        var first  = ValueReturning("first");
        var second = ValueReturning("second");
        var result = _sut.Build([first, second], AllAllowed);

        Assert.Equal("first",  result[0].Delegate.ParameterName);
        Assert.Equal("second", result[1].Delegate.ParameterName);
    }
}
