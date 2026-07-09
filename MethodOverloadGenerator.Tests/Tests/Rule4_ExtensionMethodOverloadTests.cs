namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Rule 4 — Extension method → overload for Task&lt;T&gt; / ValueTask&lt;T&gt; receiver.
///
/// For any extension method the generator adds overloads where the `this` parameter is
/// wrapped in Task&lt;T&gt; and ValueTask&lt;T&gt;.  The generated overloads are always async.
/// The method name is never changed (no "Async" suffix).
/// </summary>
public class Rule4_ExtensionMethodOverloadTests
{
    // -----------------------------------------------------------------------------------------
    // Async extension method (returns Task<TOut>)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncExtensionMethod_GeneratesTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    [Fact]
    public void AsyncExtensionMethod_GeneratesValueTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    [Fact]
    public void AsyncExtensionMethod_TaskThisOverload_BodyAwaitsInputThenCallsOriginal()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal, string mood) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        // The receiver is awaited once, then the resulting value calls the original method — the
        // outer "await" unwraps the original method's own Task<IPrey> return value.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal, string mood) where TCarnivore : ICarnivore => await (await animal).Feed(mood);");
    }

    [Fact]
    public void AsyncExtensionMethod_ValueTaskThisOverload_BodyAwaitsInputThenCallsOriginal()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal, string mood) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> animal, string mood) where TCarnivore : ICarnivore => await (await animal).Feed(mood);");
    }

    // -----------------------------------------------------------------------------------------
    // Non-async extension method (returns TOut synchronously)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncExtensionMethod_GeneratesAsyncTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public class Cat {}
            public class Mouse {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<Mouse> GetFood<TCat>(this Task<TCat> cat) where TCat : Cat => (await cat).GetFood();");
    }

    [Fact]
    public void SyncExtensionMethod_GeneratesAsyncValueTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public class Cat {}
            public class Mouse {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<Mouse> GetFood<TCat>(this ValueTask<TCat> cat) where TCat : Cat => (await cat).GetFood();");
    }

    [Fact]
    public void SyncExtensionMethod_TaskThisOverload_IsAsync_BodyAwaitsInput()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public sealed class Cat {}
            public class Mouse {}
            """);

        // Sealed receiver → no generic synthesized; the body has exactly one await (on the
        // input), not a second await wrapping the (already-synchronous) call to GetFood().
        var source = result.SingleGeneratedSource;
        TestContextHelper.NormalisedContains(source,
            "public static async Task<Mouse> GetFood(this Task<Cat> cat) => (await cat).GetFood();");
        Assert.DoesNotContain("await (await cat).GetFood()", source);
    }

    // -----------------------------------------------------------------------------------------
    // Return type handling
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ExtensionMethod_TaskReturn_OverloadsReturnTask()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    [Fact]
    public void ExtensionMethod_SyncReturn_TaskThisOverload_ReturnTypeWrappedInTask()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public sealed class Cat {}
            public class Mouse {}
            """);

        // Original returns "Mouse" synchronously — the overload must return "Task<Mouse>", not "Mouse".
        TestContextHelper.NormalisedContains(result.SingleGeneratedSource,
            "public static async Task<Mouse> GetFood(this Task<Cat> cat) => (await cat).GetFood();");
    }

    [Fact]
    public void ExtensionMethod_VoidReturn_TaskThisOverload_ReturnsTask()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static void Sedate(this Cat cat) => throw new NotImplementedException();
            }
            public sealed class Cat {}
            """);

        TestContextHelper.NormalisedContains(result.SingleGeneratedSource,
            "public static async Task Sedate(this Task<Cat> cat) => (await cat).Sedate();");
    }

    // -----------------------------------------------------------------------------------------
    // Method name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ExtensionMethod_TaskThisOverload_MethodNameUnchanged_NoAsyncSuffix()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var source = result.SingleGeneratedSource;
        Assert.Contains(" Feed<TCarnivore>(", source);
        Assert.DoesNotContain("FeedAsync", source);
    }

    // -----------------------------------------------------------------------------------------
    // Generic type parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GenericExtensionMethod_TypeParametersPreservedOnTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class IdentityExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static TIn Identity<TIn>(this TIn input) => input;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<TIn> Identity<TIn>(this Task<TIn> input) => (await input).Identity();");
    }

    [Fact]
    public void GenericExtensionMethod_TypeParametersPreservedOnValueTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class IdentityExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static TIn Identity<TIn>(this TIn input) => input;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<TIn> Identity<TIn>(this ValueTask<TIn> input) => (await input).Identity();");
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 + Rule 1/2 composition
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ExtensionMethod_WithDelegateParam_Rule1AndRule4BothApply()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        // Rule 1 alone (receiver untouched, delegate synced)...
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static Task Feed(this ICarnivore animal, Func<IPrey> getPrey) => Feed(animal, () => Task.FromResult(getPrey()));");
        // ...and Rule 4 alone (receiver wrapped, delegate untouched) both exist as separate overloads.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task Feed<TCarnivore>(this Task<TCarnivore> animal, Func<Task<IPrey>> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(getPrey);");
    }

    [Fact]
    public void ExtensionMethod_WithDelegateParam_AllCombinationsOfRule1AndRule4Generated()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        // Rule 4's own receiver-only overloads (delegate untouched) — Task<T> and ValueTask<T>.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task Feed<TCarnivore>(this Task<TCarnivore> animal, Func<Task<IPrey>> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(getPrey);",
            "public static async Task Feed<TCarnivore>(this ValueTask<TCarnivore> animal, Func<Task<IPrey>> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(getPrey);");
        // Rule 5's receiver-crossed overloads (delegate ALSO substituted) — sync and fixed-value,
        // each crossed with both receiver wrappers.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task Feed<TCarnivore>(this Task<TCarnivore> animal, Func<IPrey> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(() => Task.FromResult(getPrey()));",
            "public static async Task Feed<TCarnivore>(this ValueTask<TCarnivore> animal, Func<IPrey> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(() => Task.FromResult(getPrey()));");
    }

    [Fact]
    public void ExtensionMethod_WithMultipleDelegateParams_Rule4AndRule5BothApply()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        // Rule 4 alone — both delegate params untouched, receiver wrapped.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<string> Process(this Task<string> shelter, Func<Task<string>> p1, Func<Task<int>> p2) => await (await shelter).Process(p1, p2);");
        // Rule 5 crossed with the receiver — both delegate params synced AND receiver wrapped.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<string> Process(this Task<string> shelter, Func<string> p1, Func<int> p2) => await (await shelter).Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));");
    }

    // -----------------------------------------------------------------------------------------
    // Class-level attribute
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClassLevel_AllExtensionMethodsGetTaskThisOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public static partial class AnimalExtensions
            {
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
                public static Task Sleep(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();",
            "public static async Task Sleep<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Sleep();");
    }

    [Fact]
    public void ClassLevel_NonExtensionMethods_DoNotGetRule4Overloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public static partial class AnimalExtensions
            {
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
                public static Task<int> Add(int a, int b) => Task.FromResult(a + b);
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var combined = string.Join("\n", result.GeneratedSources);
        Assert.DoesNotContain("this Task<int>", combined);
        Assert.DoesNotContain("Add<", combined);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 does not apply to non-extension methods
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void RegularInstanceMethod_DoesNotGetTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        // Rule 4 is the only rule that introduces "async"/"await" — its absence confirms it
        // never fired for a method that isn't an extension method.
        var source = result.SingleGeneratedSource;
        Assert.DoesNotContain("async", source);
        Assert.DoesNotContain("await", source);
    }

    [Fact]
    public void RegularStaticMethod_DoesNotGetTaskThisOverload()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        // Rule 4 is the only rule that introduces "async"/"await" — its absence confirms it
        // never fired for a method that isn't an extension method.
        var source = result.SingleGeneratedSource;
        Assert.DoesNotContain("async", source);
        Assert.DoesNotContain("await", source);
    }
}
