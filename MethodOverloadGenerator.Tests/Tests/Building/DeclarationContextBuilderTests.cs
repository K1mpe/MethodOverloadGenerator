namespace MethodOverloadGenerator.Tests.Building;

public class DeclarationContextBuilderTests
{
    private readonly DeclarationContextBuilder _sut = new();

    // -----------------------------------------------------------------------------------------
    // Namespace
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Namespace_IsNull_WhenClassIsInGlobalNamespace()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        var result = _sut.Build(method);

        Assert.Null(result.Namespace);
    }

    [Fact]
    public void Namespace_IsCaptured_ForSingleSegmentNamespace()
    {
        var method = RoslynTestHelper.GetMethod(
            "namespace Animals { public class C { public void M(System.Action f) {} } }",
            "Animals.C", "M");

        var result = _sut.Build(method);

        Assert.Equal("Animals", result.Namespace);
    }

    [Fact]
    public void Namespace_IsCaptured_ForNestedNamespace()
    {
        var method = RoslynTestHelper.GetMethod(
            "namespace Animals.Services { public class C { public void M(System.Action f) {} } }",
            "Animals.Services.C", "M");

        var result = _sut.Build(method);

        Assert.Equal("Animals.Services", result.Namespace);
    }

    // -----------------------------------------------------------------------------------------
    // ClassName
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClassName_IsCaptured()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class AnimalShelter { public void M(System.Action f) {} }",
            "AnimalShelter", "M");

        var result = _sut.Build(method);

        Assert.Equal("AnimalShelter", result.ClassName);
    }

    // -----------------------------------------------------------------------------------------
    // MethodName
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MethodName_IsCaptured()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void FetchDog(System.Action f) {} }",
            "C", "FetchDog");

        var result = _sut.Build(method);

        Assert.Equal("FetchDog", result.MethodName);
    }

    // -----------------------------------------------------------------------------------------
    // AccessModifier
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AccessModifier_IsPublic()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.Public, _sut.Build(method).AccessModifier);
    }

    [Fact]
    public void AccessModifier_IsInternal()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { internal void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.Internal, _sut.Build(method).AccessModifier);
    }

    [Fact]
    public void AccessModifier_IsProtected()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { protected void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.Protected, _sut.Build(method).AccessModifier);
    }

    [Fact]
    public void AccessModifier_IsProtectedInternal()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { protected internal void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.ProtectedInternal, _sut.Build(method).AccessModifier);
    }

    [Fact]
    public void AccessModifier_IsPrivateProtected()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { private protected void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.PrivateProtected, _sut.Build(method).AccessModifier);
    }

    [Fact]
    public void AccessModifier_IsPrivate()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { private void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal(AccessModifier.Private, _sut.Build(method).AccessModifier);
    }

    // -----------------------------------------------------------------------------------------
    // IsStatic
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsStatic_IsTrue_ForStaticMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public static void M(System.Action f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method).IsStatic);
    }

    [Fact]
    public void IsStatic_IsFalse_ForInstanceMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method).IsStatic);
    }

    // -----------------------------------------------------------------------------------------
    // IsExtensionMethod
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsExtensionMethod_IsTrue_ForExtensionMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, System.Action f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method).IsExtensionMethod);
    }

    [Fact]
    public void IsExtensionMethod_IsFalse_ForRegularMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method).IsExtensionMethod);
    }

    [Fact]
    public void IsExtensionMethod_IsFalse_ForStaticNonExtensionMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method).IsExtensionMethod);
    }

    // -----------------------------------------------------------------------------------------
    // ReturnType
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReturnType_IsVoid_ForVoidMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.Equal("void", _sut.Build(method).ReturnType);
    }

    [Fact]
    public void ReturnType_IsCaptured_ForBuiltInType()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public int M() => 0; }",
            "C", "M");

        Assert.Equal("int", _sut.Build(method).ReturnType);
    }

    [Fact]
    public void ReturnType_IsCaptured_ForGenericTaskType()
    {
        var method = RoslynTestHelper.GetMethod(
            "using System.Threading.Tasks; public class C { public Task<int> M(System.Action f) => null; }",
            "C", "M");

        Assert.Equal("Task<int>", _sut.Build(method).ReturnType);
    }

    // -----------------------------------------------------------------------------------------
    // Parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Parameters_IsEmpty_WhenMethodHasNoParameters()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M() {} }",
            "C", "M");

        Assert.Empty(_sut.Build(method).Parameters);
    }

    [Fact]
    public void Parameters_CapturesTypeAndName()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(int count, string name) {} }",
            "C", "M");

        var result = _sut.Build(method);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("int",    result.Parameters[0].Type);
        Assert.Equal("count",  result.Parameters[0].Name);
        Assert.Equal("string", result.Parameters[1].Type);
        Assert.Equal("name",   result.Parameters[1].Name);
    }

    [Fact]
    public void Parameters_CapturesDelegateType_AsMinimallyQualified()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Func<int> f) {} }",
            "C", "M");

        var result = _sut.Build(method);

        Assert.Single(result.Parameters);
        Assert.Equal("Func<int>", result.Parameters[0].Type);
        Assert.Equal("f",         result.Parameters[0].Name);
    }

    [Fact]
    public void Parameters_IncludesThisParameter_ForExtensionMethod()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static class C { public static void M(this string s, int n) {} }",
            "C", "M");

        var result = _sut.Build(method);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("string", result.Parameters[0].Type);
        Assert.Equal("s",      result.Parameters[0].Name);
    }

    // -----------------------------------------------------------------------------------------
    // IsPartialClass
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsPartialClass_IsTrue_ForPartialClass()
    {
        var method = RoslynTestHelper.GetMethod(
            "public partial class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method).IsPartialClass);
    }

    [Fact]
    public void IsPartialClass_IsFalse_ForNonPartialClass()
    {
        var method = RoslynTestHelper.GetMethod(
            "public class C { public void M(System.Action f) {} }",
            "C", "M");

        Assert.False(_sut.Build(method).IsPartialClass);
    }

    [Fact]
    public void IsPartialClass_IsTrue_ForPartialStaticClass()
    {
        var method = RoslynTestHelper.GetMethod(
            "public static partial class C { public static void M(System.Action f) {} }",
            "C", "M");

        Assert.True(_sut.Build(method).IsPartialClass);
    }
}
