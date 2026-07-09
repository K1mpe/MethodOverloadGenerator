namespace MethodOverloadGenerator.Tests.Building.Rules;

public class Rule4ContextBuilderTests
{
    private readonly Rule4ContextBuilder _sut = new();

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true, AllowRule4 = true, AllowRule5 = true,
    };

    private static AllowedRulesContext Rule4Disabled => AllAllowed with { AllowRule4 = false };

    // -----------------------------------------------------------------------------------------
    // Eligibility guard
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnsNull_WhenMethodIsNotAnExtensionMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.Null(_sut.Build(method, AllAllowed));
    }

    [Fact]
    public void ReturnsNull_WhenRule4IsNotAllowed()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.Null(_sut.Build(method, Rule4Disabled));
    }

    [Fact]
    public void ReturnsContext_WhenMethodIsExtension_AndRule4IsAllowed()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.NotNull(_sut.Build(method, AllAllowed));
    }

    // -----------------------------------------------------------------------------------------
    // ThisParameterType
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThisParameterType_MatchesFirstParameterType_ForBuiltInType()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.Equal("string", _sut.Build(method, AllAllowed)!.ThisParameterType);
    }

    [Fact]
    public void ThisParameterType_MatchesFirstParameterType_ForUserDefinedType()
    {
        var method = RoslynTestHelper.GetMethod("""
            public class Dog {}
            public static class C { public static void M(this Dog d, System.Action f) {} }
            """,
            "C", "M");

        Assert.Equal("Dog", _sut.Build(method, AllAllowed)!.ThisParameterType);
    }

    // -----------------------------------------------------------------------------------------
    // ThisParameterName
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThisParameterName_MatchesFirstParameterName()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string animal, System.Action f) {} }",
            "C", "M");

        Assert.Equal("animal", _sut.Build(method, AllAllowed)!.ThisParameterName);
    }

    // -----------------------------------------------------------------------------------------
    // MethodReturnType
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MethodReturnType_IsNull_ForVoidMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.Null(_sut.Build(method, AllAllowed)!.MethodReturnType);
    }

    [Fact]
    public void MethodReturnType_IsCaptured_ForIntReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static int M(this string s, System.Action f) => 0; }",
            "C", "M");

        Assert.Equal("int", _sut.Build(method, AllAllowed)!.MethodReturnType);
    }

    [Fact]
    public void MethodReturnType_IsCaptured_ForStringReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static string M(this string s, System.Action f) => s; }",
            "C", "M");

        Assert.Equal("string", _sut.Build(method, AllAllowed)!.MethodReturnType);
    }

    [Fact]
    public void MethodReturnType_IsCaptured_ForUserDefinedReturnType()
    {
        var method = RoslynTestHelper.GetMethod("""
            public class Dog {}
            public static class C { public static Dog M(this string s, System.Action f) => null!; }
            """,
            "C", "M");

        Assert.Equal("Dog", _sut.Build(method, AllAllowed)!.MethodReturnType);
    }

    // -----------------------------------------------------------------------------------------
    // IsAsyncMethod
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsAsyncMethod_IsFalse_ForVoidMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method, AllAllowed)!.IsAsyncMethod);
    }

    [Fact]
    public void IsAsyncMethod_IsFalse_ForIntReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static int M(this string s, System.Action f) => 0; }",
            "C", "M");

        Assert.False(_sut.Build(method, AllAllowed)!.IsAsyncMethod);
    }

    [Fact]
    public void IsAsyncMethod_IsTrue_ForTaskReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static System.Threading.Tasks.Task M(this string s, System.Action f) => null!; }",
            "C", "M");

        Assert.True(_sut.Build(method, AllAllowed)!.IsAsyncMethod);
    }

    [Fact]
    public void IsAsyncMethod_IsTrue_ForTaskOfTReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static System.Threading.Tasks.Task<int> M(this string s, System.Action f) => null!; }",
            "C", "M");

        Assert.True(_sut.Build(method, AllAllowed)!.IsAsyncMethod);
    }

    [Fact]
    public void IsAsyncMethod_IsTrue_ForValueTaskReturningMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static System.Threading.Tasks.ValueTask M(this string s, System.Action f) => default; }",
            "C", "M");

        Assert.True(_sut.Build(method, AllAllowed)!.IsAsyncMethod);
    }
}
