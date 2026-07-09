namespace MethodOverloadGenerator.Tests.Building.Rules;

public class Rule1ContextsBuilderTests
{
    private readonly Rule1ContextsBuilder _sut = new();

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true, AllowRule4 = true, AllowRule5 = true,
    };

    private static AllowedRulesContext Rule1Disabled => AllAllowed with { AllowRule1 = false };

    private static DelegateInfo AsyncDelegate(string name = "f") => new()
    {
        ParameterName = name,
        InputTypes    = [],
        ReturnType    = null,
        IsAsync       = true,
        IsValueTask   = false,
    };

    private static DelegateInfo SyncDelegate(string name = "f") => new()
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
    public void ReturnsEmpty_WhenDelegateIsNotAsync()
    {
        Assert.Empty(_sut.Build([SyncDelegate()], AllAllowed));
    }

    [Fact]
    public void ReturnsEmpty_WhenRule1IsNotAllowed()
    {
        Assert.Empty(_sut.Build([AsyncDelegate()], Rule1Disabled));
    }

    // -----------------------------------------------------------------------------------------
    // Inclusion
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsOneEntry_ForSingleAsyncDelegate_WhenAllowed()
    {
        Assert.Single(_sut.Build([AsyncDelegate()], AllAllowed));
    }

    [Fact]
    public void ReturnsOnlyAsyncDelegates_WhenMixed()
    {
        var result = _sut.Build([SyncDelegate("sync"), AsyncDelegate("async")], AllAllowed);

        Assert.Single(result);
        Assert.Equal("async", result[0].Delegate.ParameterName);
    }

    [Fact]
    public void ReturnsMultipleEntries_ForMultipleAsyncDelegates()
    {
        Assert.Equal(2, _sut.Build([AsyncDelegate("a"), AsyncDelegate("b")], AllAllowed).Count);
    }

    // -----------------------------------------------------------------------------------------
    // Context content
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ResultEntry_ContainsOriginalDelegateInfo()
    {
        var delegate1 = AsyncDelegate("fetchDog");
        var result = _sut.Build([delegate1], AllAllowed);

        Assert.Same(delegate1, result[0].Delegate);
    }

    [Fact]
    public void ResultEntries_PreserveDeclarationOrder()
    {
        var first  = AsyncDelegate("first");
        var second = AsyncDelegate("second");
        var result = _sut.Build([first, second], AllAllowed);

        Assert.Equal("first",  result[0].Delegate.ParameterName);
        Assert.Equal("second", result[1].Delegate.ParameterName);
    }
}
