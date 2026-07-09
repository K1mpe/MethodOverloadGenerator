namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Rule 2 — Value-returning delegate → fixed-value overload.
///
/// When the delegate returns any value (Task&lt;T&gt;, ValueTask&lt;T&gt;, or plain T) the generator
/// adds an overload that accepts a plain T directly so callers can pass a constant without
/// wrapping it in a lambda.
/// </summary>
public class Rule2_FixedValueOverloadTests
{
    private static string Generate(string source) => TestHelper.RunGenerator(source).SingleGeneratedSource;

    // -----------------------------------------------------------------------------------------
    // Func<Task<T>> → T  (alongside the Rule 1 sync overload)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTaskT_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("");
            }
            """);

        Assert.Contains("string fetchValue", source);
    }

    [Fact]
    public void FuncTaskT_FixedValueOverload_BodyWrapsWithTaskFromResult()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("");
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<string> Process(string fetchValue) => Process(() => Task.FromResult(fetchValue));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Func<ValueTask<T>> → T
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncValueTaskT_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetch) => new(string.Empty);
            }
            """);

        Assert.Contains("string fetchValue", source);
    }

    [Fact]
    public void FuncValueTaskT_FixedValueOverload_BodyWrapsWithNewValueTask()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetch) => new(string.Empty);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public ValueTask<string> Process(string fetchValue) => Process(() => new ValueTask<string>(fetchValue));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Func<T> (sync delegate) → T
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncT_Sync_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<string> fetch) => fetch();
            }
            """);

        Assert.Contains("string fetchValue", source);
    }

    [Fact]
    public void FuncT_Sync_FixedValueOverload_BodyWrapsAsConstantLambda()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<string> fetch) => fetch();
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public string Process(string fetchValue) => Process(() => fetchValue);
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Delegates that produce no value — no fixed-value overload should be generated
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTask_Void_DoesNotGenerateFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action, Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("actionValue", source);
    }

    [Fact]
    public void FuncValueTask_Void_DoesNotGenerateFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<ValueTask> action, Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("actionValue", source);
    }

    [Fact]
    public void Action_DoesNotGenerateFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Action doIt, Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("doItValue", source);
    }

    [Fact]
    public void ActionWithParams_DoesNotGenerateFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Action<int, string> handle, Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("handleValue", source);
    }

    // -----------------------------------------------------------------------------------------
    // Return type variety
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTaskString_GeneratesStringFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetchName) => Task.FromResult("");
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<string> Process(string fetchNameValue) => Process(() => Task.FromResult(fetchNameValue));
            """);
    }

    [Fact]
    public void FuncTaskReferenceType_GeneratesCorrectFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<Animal> Process(Func<Task<Animal>> fetch) => Task.FromResult(new Animal());
            }
            public class Animal {}
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<Animal> Process(Animal fetchValue) => Process(() => Task.FromResult(fetchValue));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Fixed-value overload is distinct from the sync overload produced by Rule 1
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncDelegate_BothSyncAndFixedValueOverloadsGenerated_AreDistinctMethods()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetch) => Process(() => Task.FromResult(fetch()));
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(int fetchValue) => Process(() => Task.FromResult(fetchValue));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegates
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ReturningTaskT_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchNumberAsync();
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(FetchNumberAsync fetch) => Task.FromResult(0);
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(int fetchValue) => Process(() => Task.FromResult(fetchValue));
            """);
    }

    [Fact]
    public void NamedDelegate_ReturningT_GeneratesFixedValueOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public delegate int FetchNumber();
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public int Process(FetchNumber fetch) => fetch();
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public int Process(int fetchValue) => Process(() => fetchValue);
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute placement
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule2_TriggeredByParameterLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", source);
    }

    [Fact]
    public void Rule2_TriggeredByMethodLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", source);
    }

    [Fact]
    public void Rule2_TriggeredByClassLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", source);
    }
}
