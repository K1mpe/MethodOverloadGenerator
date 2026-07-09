namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Rule 3 — Multi-parameter delegate → trailing-parameter overloads.
///
/// When the delegate has multiple input parameters the generator creates additional overloads
/// that progressively drop trailing parameters one by one.  All rules compose, so an async
/// multi-parameter delegate also gets the full sync matrix from Rule 1.
/// </summary>
public class Rule3_TrailingParameterOverloadTests
{
    private static string Generate(string source) => TestHelper.RunGenerator(source).SingleGeneratedSource;

    // Counts how many generated overloads forward to the original method — one occurrence of
    // "=> Process(" per overload body, regardless of which rule produced it.
    private static int CountOverloads(string source) => source.Split("=> Process(").Length - 1;

    // -----------------------------------------------------------------------------------------
    // Two input parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TwoInputParams_AsyncReturn_GeneratesOneInputParamAsyncOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", source);
    }

    [Fact]
    public void TwoInputParams_AsyncReturn_GeneratesZeroInputParamAsyncOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<Task<bool>> canAdmit", source);
    }

    [Fact]
    public void TwoInputParams_AsyncReturn_GeneratesAllThreeSyncVariants()
        // Func<A, B, Task<T>> → Func<A,B,T> and Func<A,T> and Func<T>
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, string, bool> canAdmit", source);
        Assert.Contains("Func<int, bool> canAdmit", source);
        Assert.Contains("Func<bool> canAdmit", source);
    }

    [Fact]
    public void TwoInputParams_AsyncReturn_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("bool canAdmitValue", source);
    }

    [Fact]
    public void TwoInputParams_TaskVoidReturn_GeneratesAllActionVariants()
        // Func<A, B, Task> → Action<A,B>, Action<A>, Action
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task> notify) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action<int, string> notify", source);
        Assert.Contains("Action<int> notify", source);
        Assert.Contains("Action notify", source);
    }

    [Fact]
    public void TwoInputParams_ValueTaskReturn_GeneratesCorrectSyncVariants()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<bool> Process(Func<int, string, ValueTask<bool>> canAdmit) => new(true);
            }
            """);

        Assert.Contains("Func<int, string, bool> canAdmit", source);
        Assert.Contains("Func<int, bool> canAdmit", source);
        Assert.Contains("Func<bool> canAdmit", source);
    }

    // -----------------------------------------------------------------------------------------
    // Three input parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThreeInputParams_AsyncReturn_GeneratesAllAsyncVariants()
        // Drops 3→2, 3→1, 3→0 trailing params for async overloads
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<double> Process(Func<int, string, bool, Task<double>> compute) => Task.FromResult(0.0);
            }
            """);

        Assert.Contains("Func<int, string, Task<double>> compute", source);
        Assert.Contains("Func<int, Task<double>> compute", source);
        Assert.Contains("Func<Task<double>> compute", source);
    }

    [Fact]
    public void ThreeInputParams_AsyncReturn_GeneratesAllSyncVariants()
        // Sync: 3,2,1,0 input params
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<double> Process(Func<int, string, bool, Task<double>> compute) => Task.FromResult(0.0);
            }
            """);

        Assert.Contains("Func<int, string, bool, double> compute", source);
        Assert.Contains("Func<int, string, double> compute", source);
        Assert.Contains("Func<int, double> compute", source);
        Assert.Contains("Func<double> compute", source);
    }

    [Fact]
    public void ThreeInputParams_AsyncReturn_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<double> Process(Func<int, string, bool, Task<double>> compute) => Task.FromResult(0.0);
            }
            """);

        Assert.Contains("double computeValue", source);
    }

    // -----------------------------------------------------------------------------------------
    // Single input parameter — Rule 3 produces no additional variants
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SingleInputParam_AsyncReturn_NoAdditionalTrailingVariantsFromRule3()
        // Rule 1 and 2 still apply, but with only one input parameter there's no intermediate
        // arity to drop to — Rule 3 can only collapse straight from 1 input down to 0, i.e. the
        // async-drop and fully-sync zero-input forms, and nothing in between.
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, bool> canAdmit", source);   // Rule 1 — full-arity sync
        Assert.Contains("bool canAdmitValue", source);          // Rule 2 — fixed value
        Assert.Contains("Func<Task<bool>> canAdmit", source);   // Rule 3 — async-drop to zero
        Assert.Contains("Func<bool> canAdmit", source);         // Rule 3 — fully-sync at zero

        // Rule 1 (1) + Rule 2 (1) + Rule 3's single drop-to-zero step (2) = 4 — no additional
        // intermediate arities exist for a single-input delegate.
        Assert.Equal(4, CountOverloads(source));
    }

    // -----------------------------------------------------------------------------------------
    // No input parameters — Rule 3 does not apply
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ZeroInputParams_AsyncReturn_Rule3DoesNotApply()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        // Only Rule 1 (sync) and Rule 2 (fixed value) apply — Rule 3 needs at least one input
        // parameter to have anything to drop.
        Assert.Equal(2, CountOverloads(source));
    }

    // -----------------------------------------------------------------------------------------
    // Generated overload bodies
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TrailingParamOverloads_AsyncVariants_DelegateToOriginalWithCorrectArity()
        // e.g. (a, b) => func(a) — trailing param dropped in forwarding call
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<bool> Process(Func<int, Task<bool>> canAdmit) => Process((a, b) => canAdmit(a));
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<bool> Process(Func<Task<bool>> canAdmit) => Process((a, b) => canAdmit());
            """);
    }

    [Fact]
    public void TrailingParamOverloads_SyncVariants_WrappedWithTaskFromResult()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<bool> Process(Func<int, bool> canAdmit) => Process((a, b) => Task.FromResult(canAdmit(a)));
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<bool> Process(Func<bool> canAdmit) => Process((a, b) => Task.FromResult(canAdmit()));
            """);
    }

    [Fact]
    public void TrailingParamOverloads_FixedValue_AllInputParamsIgnored()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<bool> Process(bool canAdmitValue) => Process((int p0, string p1) => Task.FromResult(canAdmitValue));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Correct total number of overloads generated
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TwoInputParams_AsyncReturn_CorrectTotalNumberOfOverloadsGenerated()
        // 2 async trailing-drop + 3 sync + 1 value = 6 overloads (excluding the original)
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Equal(6, CountOverloads(source));
    }

    [Fact]
    public void ThreeInputParams_AsyncReturn_CorrectTotalNumberOfOverloadsGenerated()
        // 3 async trailing-drop + 4 sync + 1 value = 8 overloads (excluding the original)
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<double> Process(Func<int, string, bool, Task<double>> compute) => Task.FromResult(0.0);
            }
            """);

        Assert.Equal(8, CountOverloads(source));
    }

    // -----------------------------------------------------------------------------------------
    // Attribute placement
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule3_TriggeredByParameterLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<bool> Process([MethodOverloadGeneratorAttribute] Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", source);
    }

    [Fact]
    public void Rule3_TriggeredByMethodLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", source);
    }

    [Fact]
    public void Rule3_TriggeredByClassLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", source);
    }
}
