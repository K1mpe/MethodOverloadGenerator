namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Rule 1 — Async delegate → sync overload.
///
/// When a delegate returns Task&lt;T&gt; or ValueTask&lt;T&gt; the generator adds a Func&lt;T&gt; overload.
/// When a delegate returns Task or ValueTask (no return value) the generator adds an Action overload.
/// </summary>
public class Rule1_SyncOverloadTests
{
    private static string Generate(string source) => TestHelper.RunGenerator(source).SingleGeneratedSource;

    // -----------------------------------------------------------------------------------------
    // Func<Task<T>> → Func<T>
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTaskT_GeneratesSyncFuncT_Overload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("");
            }
            """);

        Assert.Contains("Func<string> fetch", source);
    }

    [Fact]
    public void FuncTaskT_SyncOverload_BodyWrapsWithTaskFromResult()
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
            public Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void FuncTaskT_SyncOverload_AllowsCallerToPassSyncLambda()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("");
            }

            public class Usage
            {
                public void Run(MyService service) => service.Process(() => "hello");
            }
            """);

        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------------------------
    // Func<ValueTask<T>> → Func<T>
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncValueTaskT_GeneratesSyncFuncT_Overload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetch) => new(string.Empty);
            }
            """);

        Assert.Contains("Func<string> fetch", source);
    }

    [Fact]
    public void FuncValueTaskT_SyncOverload_BodyWrapsWithNewValueTask()
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
            public ValueTask<string> Process(Func<string> fetch) => Process(() => new ValueTask<string>(fetch()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Func<Task> (void async) → Action
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTask_Void_GeneratesActionOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action action", source);
    }

    [Fact]
    public void FuncTask_Void_ActionOverload_BodyUsesTaskCompletedTask()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task Process(Action action) => Process(() => { action(); return Task.CompletedTask; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Func<ValueTask> (void async) → Action
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncValueTask_Void_GeneratesActionOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask Process(Func<ValueTask> action) => default;
            }
            """);

        Assert.Contains("Action action", source);
    }

    [Fact]
    public void FuncValueTask_Void_ActionOverload_BodyUsesDefaultValueTask()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask Process(Func<ValueTask> action) => default;
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public ValueTask Process(Action action) => Process(() => { action(); return default; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Multi-parameter async delegates — sync variant preserves input parameter types
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncWithOneInputParam_AsyncReturn_GeneratesSyncFuncWithSameInputParam()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<int, Task<string>> fetch) => Task.FromResult("");
            }
            """);

        Assert.Contains("Func<int, string> fetch", source);
    }

    [Fact]
    public void FuncWithOneInputParam_TaskVoidReturn_GeneratesActionWithSameInputParam()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action<int> action", source);
    }

    [Fact]
    public void FuncWithMultipleInputParams_AsyncReturn_GeneratesSyncFuncWithSameInputParams()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> fetch) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, string, bool> fetch", source);
    }

    [Fact]
    public void FuncWithMultipleInputParams_TaskVoidReturn_GeneratesActionWithSameInputParams()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action<int, string> action", source);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegates behave the same as their Func<> equivalents
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ReturningTaskT_GeneratesSyncOverload()
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
            public Task<int> Process(Func<int> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void NamedDelegate_ReturningTask_GeneratesActionOverload()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public delegate Task InitializeAsync();
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(InitializeAsync initialize) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task Process(Action initialize) => Process(() => { initialize(); return Task.CompletedTask; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute placement — Rule 1 is triggered at all levels
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule1_TriggeredByParameterLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", source);
    }

    [Fact]
    public void Rule1_TriggeredByMethodLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", source);
    }

    [Fact]
    public void Rule1_TriggeredByClassLevelAttribute()
    {
        var source = Generate("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", source);
    }
}
