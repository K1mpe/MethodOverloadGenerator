using Microsoft.CodeAnalysis;

namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Diagnostic tests — verifies that the generator emits the correct errors, warnings, and
/// silent skips for each unsupported scenario.
///
/// General principle from the spec:
///   • Explicit opt-in (attribute on method or parameter) → always an error when unsupported.
///   • Class-level opt-in → unsupported methods are silently skipped so the rest still benefit.
/// </summary>
public class DiagnosticsTests
{
    // -----------------------------------------------------------------------------------------
    // out parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void OutParam_MethodLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Func<Task<int>> fetch, out int captured) => captured = 0;
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG004", error.Id);
    }

    [Fact]
    public void OutParam_ParameterLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public void Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch, out int captured) => captured = 0;
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG004", error.Id);
    }

    [Fact]
    public void OutParam_ClassLevelAttribute_MethodSilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void Process(Func<Task<int>> fetch, out int captured) => captured = 0;
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void OutParam_ClassLevelAttribute_OtherMethodsInClassStillGetOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void WithOutParam(Func<Task<int>> fetch, out int captured) => captured = 0;
                public Task<int> WithoutOutParam(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Errors);
        var source = result.SingleGeneratedSource;
        Assert.Contains("WithoutOutParam", source);
        Assert.DoesNotContain("WithOutParam(", source);
    }

    // -----------------------------------------------------------------------------------------
    // ref parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void RefParam_MethodLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Func<Task<int>> fetch, ref int counter) { }
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG004", error.Id);
    }

    [Fact]
    public void RefParam_ParameterLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public void Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch, ref int counter) { }
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG004", error.Id);
    }

    [Fact]
    public void RefParam_ClassLevelAttribute_MethodSilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void Process(Func<Task<int>> fetch, ref int counter) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RefParam_ClassLevelAttribute_OtherMethodsInClassStillGetOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void WithRefParam(Func<Task<int>> fetch, ref int counter) { }
                public Task<int> WithoutRefParam(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Errors);
        var source = result.SingleGeneratedSource;
        Assert.Contains("WithoutRefParam", source);
        Assert.DoesNotContain("WithRefParam(", source);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute on a non-delegate parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonDelegateParam_ParameterLevelAttribute_ReportsCompileError()
        // e.g. [MethodOverloadGenerator] int capacity — int is not a delegate
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] int capacity) => Task.FromResult(capacity);
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG005", error.Id);
    }

    [Fact]
    public void NonDelegateParam_ClassLevelAttribute_MethodSilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> OnlyNonDelegateParams(int capacity) => Task.FromResult(capacity);
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void NonDelegateParam_ClassLevelAttribute_OtherDelegateMethodsStillGetOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> OnlyNonDelegateParams(int capacity) => Task.FromResult(capacity);
                public Task<int> WithDelegate(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Contains("WithDelegate", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // No overloads would be generated (e.g. Action with no parameters)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ActionWithNoParams_MethodLevelAttribute_ReportsWarning()
        // Action has no return value and no parameters — no rule applies
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Action action) { }
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG001", warning.Id);
    }

    [Fact]
    public void ActionWithNoParams_ParameterLevelAttribute_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public void Process([MethodOverloadGeneratorAttribute] Action action) { }
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG001", warning.Id);
    }

    [Fact]
    public void ActionWithNoParams_ClassLevelAttribute_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void Process(Action action) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void MethodWithNoDelegateParams_MethodLevelAttribute_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(int capacity) => Task.FromResult(capacity);
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG001", warning.Id);
    }

    [Fact]
    public void NoOverloadsGenerated_WarningPointsAtTheAttribute()
        // Diagnostics don't carry real source locations yet (all use Location.None), so the only
        // way a consumer can currently identify *which* method the warning is about is the
        // message text — verify it names the attributed method.
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Action action) { }
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Contains("Process", warning.GetMessage());
    }

    // -----------------------------------------------------------------------------------------
    // Non-partial class
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonPartialClass_ParameterLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG003");
    }

    [Fact]
    public void NonPartialClass_MethodLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG003");
    }

    [Fact]
    public void NonPartialClass_ClassLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public class MyService
            {
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG003");
    }

    [Fact]
    public void NonPartialStaticExtensionClass_ClassLevelAttribute_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public static class MyExtensions
            {
                public static Task<int> Process(this string s, Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG003");
    }

    [Fact]
    public void PartialClass_NoErrorOrWarning()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Diagnostics);
    }

    // -----------------------------------------------------------------------------------------
    // Error location / message quality
    //
    // Every diagnostic is reported at the location of the [MethodOverloadGenerator] attribute
    // usage itself — the one site common to all three placement levels (class, method/constructor,
    // parameter), so these assert that the location is a real source location pointing at the
    // attribute, rather than Location.None.
    // -----------------------------------------------------------------------------------------

    private static string TextAt(Location location)
    {
        Assert.Equal(LocationKind.SourceFile, location.Kind);
        return location.SourceTree!.GetText().GetSubText(location.SourceSpan).ToString();
    }

    [Fact]
    public void OutParam_Error_ReportedAtAttributeOrMethodSite()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Func<Task<int>> fetch, out int captured) => captured = 0;
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG004", error.Id);
        Assert.Contains("MethodOverloadGeneratorAttribute", TextAt(error.Location));
    }

    [Fact]
    public void NonPartialClass_Error_ReportedAtClassOrAttributeSite()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public class MyService
            {
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG003", error.Id);
        Assert.Contains("MethodOverloadGeneratorAttribute", TextAt(error.Location));
    }

    [Fact]
    public void NonDelegateParam_Error_ReportedAtAttributeSite()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] int capacity) => Task.FromResult(capacity);
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG005", error.Id);
        Assert.Contains("MethodOverloadGeneratorAttribute", TextAt(error.Location));
    }
}
