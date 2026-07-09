namespace MethodOverloadGenerator.Tests.Building;

public class DelegateParametersBuilderTests
{
    private readonly DelegateParametersBuilder _sut = new();

    // -----------------------------------------------------------------------------------------
    // Parameter count / inclusion
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsEmpty_WhenMethodHasNoDelegateParameters()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(string name, int count) {} }",
            "C", "M");

        Assert.Empty(_sut.Build(method));
    }

    [Fact]
    public void ReturnsOneEntry_ForSingleDelegateParameter()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action callback) {} }",
            "C", "M");

        Assert.Single(_sut.Build(method));
    }

    [Fact]
    public void NonDelegateParametersAreExcluded()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(string name, System.Action callback) {} }",
            "C", "M");

        Assert.Single(_sut.Build(method));
    }

    [Fact]
    public void MultipleDelgateParametersAreAllIncluded()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action a, System.Action b) {} }",
            "C", "M");

        Assert.Equal(2, _sut.Build(method).Count);
    }

    [Fact]
    public void ExtensionThisParameter_IsExcluded()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this System.Action receiver, System.Action callback) {} }",
            "C", "M");

        // The 'this' parameter is a delegate but must not appear in the result;
        // only the regular delegate params should be included.
        Assert.Single(_sut.Build(method));
    }

    [Fact]
    public void DelegateParametersAreReturnedInDeclarationOrder()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action first, System.Action second) {} }",
            "C", "M");

        var result = _sut.Build(method);

        Assert.Equal("first",  result[0].ParameterName);
        Assert.Equal("second", result[1].ParameterName);
    }

    // -----------------------------------------------------------------------------------------
    // ParameterName
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ParameterName_MatchesSourceParameterName()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<int> fetchCount) {} }",
            "C", "M");

        Assert.Equal("fetchCount", _sut.Build(method)[0].ParameterName);
    }

    // -----------------------------------------------------------------------------------------
    // InputTypes
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void InputTypes_IsEmpty_ForActionWithNoTypeArguments()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.Empty(_sut.Build(method)[0].InputTypes);
    }

    [Fact]
    public void InputTypes_IsEmpty_ForFuncWithOnlyReturnType()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<int> f) {} }",
            "C", "M");

        Assert.Empty(_sut.Build(method)[0].InputTypes);
    }

    [Fact]
    public void InputTypes_CapturesSingleInputType()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action<string> f) {} }",
            "C", "M");

        Assert.Equal(["string"], _sut.Build(method)[0].InputTypes);
    }

    [Fact]
    public void InputTypes_CapturesMultipleInputTypes_InOrder()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<string, bool, int> f) {} }",
            "C", "M");

        // Func<string, bool, int> → inputs are string, bool; return is int
        Assert.Equal(["string", "bool"], _sut.Build(method)[0].InputTypes);
    }

    // -----------------------------------------------------------------------------------------
    // ReturnType
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnType_IsNull_ForAction()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.Null(_sut.Build(method)[0].ReturnType);
    }

    [Fact]
    public void ReturnType_IsNull_ForFuncReturningTask()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.Task> f) {} }",
            "C", "M");

        // Task is the async-void equivalent — unwrapped return type is null
        Assert.Null(_sut.Build(method)[0].ReturnType);
    }

    [Fact]
    public void ReturnType_IsNull_ForFuncReturningValueTask()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.ValueTask> f) {} }",
            "C", "M");

        Assert.Null(_sut.Build(method)[0].ReturnType);
    }

    [Fact]
    public void ReturnType_IsCaptured_ForFuncReturningInt()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<int> f) {} }",
            "C", "M");

        Assert.Equal("int", _sut.Build(method)[0].ReturnType);
    }

    [Fact]
    public void ReturnType_IsCaptured_ForFuncReturningTaskOfString()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.Task<string>> f) {} }",
            "C", "M");

        Assert.Equal("string", _sut.Build(method)[0].ReturnType);
    }

    [Fact]
    public void ReturnType_IsCaptured_ForFuncReturningValueTaskOfInt()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.ValueTask<int>> f) {} }",
            "C", "M");

        Assert.Equal("int", _sut.Build(method)[0].ReturnType);
    }

    // -----------------------------------------------------------------------------------------
    // IsAsync / IsValueTask
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsAsync_IsFalse_ForActionDelegate()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method)[0].IsAsync);
    }

    [Fact]
    public void IsAsync_IsFalse_ForFuncReturningPlainInt()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<int> f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method)[0].IsAsync);
    }

    [Fact]
    public void IsAsync_IsTrue_ForFuncReturningTask()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.Task> f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method)[0].IsAsync);
    }

    [Fact]
    public void IsAsync_IsTrue_ForFuncReturningTaskOfT()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.Task<int>> f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method)[0].IsAsync);
    }

    [Fact]
    public void IsAsync_IsTrue_ForFuncReturningValueTask()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.ValueTask> f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method)[0].IsAsync);
    }

    [Fact]
    public void IsValueTask_IsFalse_ForTaskDelegate()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.Task> f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method)[0].IsValueTask);
    }

    [Fact]
    public void IsValueTask_IsTrue_ForValueTaskDelegate()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.ValueTask> f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method)[0].IsValueTask);
    }

    [Fact]
    public void IsValueTask_IsTrue_ForValueTaskOfTDelegate()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<System.Threading.Tasks.ValueTask<int>> f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method)[0].IsValueTask);
    }
}
