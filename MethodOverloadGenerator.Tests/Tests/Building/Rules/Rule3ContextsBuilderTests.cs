namespace MethodOverloadGenerator.Tests.Building.Rules;

public class Rule3ContextsBuilderTests
{
    private readonly Rule3ContextsBuilder _sut = new();

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true, AllowRule4 = true, AllowRule5 = true,
    };

    private static AllowedRulesContext Rule3Disabled => AllAllowed with { AllowRule3 = false };

    private static DelegateInfo MultiInputDelegate(string name = "f", int inputCount = 2) => new()
    {
        ParameterName = name,
        InputTypes    = Enumerable.Range(0, inputCount).Select(i => $"T{i}").ToList(),
        ReturnType    = null,
        IsAsync       = false,
        IsValueTask   = false,
    };

    private static DelegateInfo SingleInputDelegate(string name = "f") => new()
    {
        ParameterName = name,
        InputTypes    = ["string"],
        ReturnType    = null,
        IsAsync       = false,
        IsValueTask   = false,
    };

    private static DelegateInfo ZeroInputDelegate(string name = "f") => new()
    {
        ParameterName = name,
        InputTypes    = [],
        ReturnType    = null,
        IsAsync       = false,
        IsValueTask   = false,
    };

    private static DelegateInfo AsyncMultiInputDelegate(string name = "f", int inputCount = 2) => new()
    {
        ParameterName = name,
        InputTypes    = Enumerable.Range(0, inputCount).Select(i => $"T{i}").ToList(),
        ReturnType    = "TResult",
        IsAsync       = true,
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
    public void ReturnsEmpty_WhenDelegateHasZeroInputTypes()
    {
        Assert.Empty(_sut.Build([ZeroInputDelegate()], AllAllowed));
    }

    [Fact]
    public void ReturnsOneEntry_ForSingleInputDelegate()
    {
        // N=1 → only k=0 (drop the single input) → 1 context
        var result = _sut.Build([SingleInputDelegate()], AllAllowed);

        Assert.Single(result);
        Assert.Equal(0, result[0].TargetInputCount);
    }

    [Fact]
    public void ReturnsEmpty_WhenRule3IsNotAllowed()
    {
        Assert.Empty(_sut.Build([MultiInputDelegate()], Rule3Disabled));
    }

    // -----------------------------------------------------------------------------------------
    // Count — one context per (delegate, k) pair
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsTwoEntries_ForTwoInputDelegate()
    {
        // N=2 → k=1 and k=0 → 2 contexts
        Assert.Equal(2, _sut.Build([MultiInputDelegate()], AllAllowed).Count);
    }

    [Fact]
    public void ReturnsThreeEntries_ForThreeInputDelegate()
    {
        // N=3 → k=2, k=1, k=0 → 3 contexts
        Assert.Equal(3, _sut.Build([MultiInputDelegate(inputCount: 3)], AllAllowed).Count);
    }

    [Fact]
    public void ReturnsOnlyMultiInputDelegates_WhenMixed()
    {
        var result = _sut.Build([ZeroInputDelegate("zero"), MultiInputDelegate("multi")], AllAllowed);

        // multi has N=2 → 2 contexts; zero is excluded
        Assert.Equal(2, result.Count);
        Assert.All(result, ctx => Assert.Equal("multi", ctx.Delegate.ParameterName));
    }

    [Fact]
    public void ReturnsFourEntries_ForTwoTwoInputDelegates()
    {
        // 2 delegates × 2 k-values each = 4 contexts
        Assert.Equal(4, _sut.Build([MultiInputDelegate("a"), MultiInputDelegate("b")], AllAllowed).Count);
    }

    // -----------------------------------------------------------------------------------------
    // TargetInputCount values
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FirstContext_HasTargetInputCount_N_Minus_1()
    {
        var result = _sut.Build([MultiInputDelegate()], AllAllowed);

        Assert.Equal(1, result[0].TargetInputCount); // N=2, first k = N-1 = 1
    }

    [Fact]
    public void LastContext_HasTargetInputCount_Zero()
    {
        var result = _sut.Build([MultiInputDelegate()], AllAllowed);

        Assert.Equal(0, result[^1].TargetInputCount);
    }

    [Fact]
    public void ContextsOrderedByDecreasingTargetInputCount()
    {
        var result = _sut.Build([MultiInputDelegate(inputCount: 3)], AllAllowed);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].TargetInputCount);
        Assert.Equal(1, result[1].TargetInputCount);
        Assert.Equal(0, result[2].TargetInputCount);
    }

    // -----------------------------------------------------------------------------------------
    // Context content and ordering across delegates
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ResultEntry_ContainsOriginalDelegateInfo()
    {
        var delegate1 = MultiInputDelegate("process");
        var result = _sut.Build([delegate1], AllAllowed);

        Assert.All(result, ctx => Assert.Same(delegate1, ctx.Delegate));
    }

    [Fact]
    public void ResultEntries_AreOrderedByDelegateThenByDecreasingK()
    {
        var first  = MultiInputDelegate("first");
        var second = MultiInputDelegate("second");
        var result = _sut.Build([first, second], AllAllowed);

        Assert.Equal(4, result.Count);
        Assert.Equal("first",  result[0].Delegate.ParameterName);
        Assert.Equal(1,        result[0].TargetInputCount);
        Assert.Equal("first",  result[1].Delegate.ParameterName);
        Assert.Equal(0,        result[1].TargetInputCount);
        Assert.Equal("second", result[2].Delegate.ParameterName);
        Assert.Equal(1,        result[2].TargetInputCount);
        Assert.Equal("second", result[3].Delegate.ParameterName);
        Assert.Equal(0,        result[3].TargetInputCount);
    }

    // -----------------------------------------------------------------------------------------
    // PreserveAsync — async delegates get both an async-drop and a fully-sync context per k,
    // since "fewer parameters" and "sync return" are independently useful and must also be
    // combinable with another parameter's own substitution via Rule 5
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonAsyncDelegate_EveryContext_HasPreserveAsyncFalse()
    {
        var result = _sut.Build([MultiInputDelegate()], AllAllowed);

        Assert.All(result, ctx => Assert.False(ctx.PreserveAsync));
    }

    [Fact]
    public void AsyncDelegate_ReturnsTwoContextsPerK_OneForEachForm()
    {
        // N=2 → k=1 and k=0, each with 2 forms (async-drop, fully-sync) → 4 contexts
        var result = _sut.Build([AsyncMultiInputDelegate()], AllAllowed);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void AsyncDelegate_AtEachK_HasOnePreserveAsyncTrue_AndOnePreserveAsyncFalse()
    {
        var result = _sut.Build([AsyncMultiInputDelegate(inputCount: 3)], AllAllowed);

        foreach (var k in new[] { 2, 1, 0 })
        {
            var atK = result.Where(c => c.TargetInputCount == k).ToList();
            Assert.Equal(2, atK.Count);
            Assert.Contains(atK, c => c.PreserveAsync);
            Assert.Contains(atK, c => !c.PreserveAsync);
        }
    }

    [Fact]
    public void AsyncSingleInputDelegate_ReturnsBothForms_AtKEqualsZero()
    {
        // N=1 → only k=0, but still both forms → 2 contexts
        var result = _sut.Build([new DelegateInfo
        {
            ParameterName = "f",
            InputTypes    = ["int"],
            ReturnType    = "TResult",
            IsAsync       = true,
            IsValueTask   = false,
        }], AllAllowed);

        Assert.Equal(2, result.Count);
        Assert.All(result, ctx => Assert.Equal(0, ctx.TargetInputCount));
        Assert.Contains(result, c => c.PreserveAsync);
        Assert.Contains(result, c => !c.PreserveAsync);
    }
}
