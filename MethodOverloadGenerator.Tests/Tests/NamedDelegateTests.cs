namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Tests for named (user-defined) delegates, e.g. <c>public delegate Task&lt;IPrey&gt; HuntAsync()</c>.
///
/// Named delegates must be treated identically to their equivalent Func&lt;&gt; / Action&lt;&gt;
/// signatures — the generator should inspect the delegate's Invoke signature rather than
/// relying on the type being a generic Func/Action.
/// </summary>
public class NamedDelegateTests
{
    private static string EmitFor(string source)
    {
        var result = TestHelper.RunGenerator(source);
        Assert.Empty(result.Errors);
        return result.SingleGeneratedSource;
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate returning Task<T>
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ReturningTaskT_GetsSyncFuncOverload()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Admit(FetchCapacity fetchCapacity) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int> fetchCapacity", source);
    }

    [Fact]
    public void NamedDelegate_ReturningTaskT_GetsFixedValueOverload()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Admit(FetchCapacity fetchCapacity) => Task.FromResult(true);
            }
            """);

        Assert.Contains("int fetchCapacityValue", source);
    }

    [Fact]
    public void NamedDelegate_ReturningTaskT_BehavesIdenticallyToEquivalentFuncTaskT()
    {
        // Generated overloads only ever mention the substituted type (Func<int>, int), never the
        // original delegate's own name — so a named delegate and its Func<> equivalent must
        // produce identical generated overload signatures.
        var named = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Admit(FetchCapacity fetchCapacity) => Task.FromResult(true);
            }
            """);

        var equivalent = EmitFor("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Admit(Func<Task<int>> fetchCapacity) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int> fetchCapacity", named);
        Assert.Contains("Func<int> fetchCapacity", equivalent);
        Assert.Contains("int fetchCapacityValue", named);
        Assert.Contains("int fetchCapacityValue", equivalent);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate returning Task (void async)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ReturningTask_GetsActionOverload()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task Initialize();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task Setup(Initialize initialize) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action initialize", source);
    }

    [Fact]
    public void NamedDelegate_ReturningTask_DoesNotGetFixedValueOverload()
    {
        // Task (no T) unwraps to a null return type, so Rule 2 (fixed value) never applies —
        // there is no value to fix, only the async wrapper to drop.
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task Initialize();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task Setup(Initialize initialize) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("initializeValue", source);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate returning a plain (non-Task) value
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ReturningT_GetsFixedValueOverload()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate int GetCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(GetCapacity getCapacity) => true;
            }
            """);

        Assert.Contains("bool Process(int getCapacityValue)", source);
    }

    [Fact]
    public void NamedDelegate_ReturningT_BehavesIdenticallyToEquivalentFuncT()
    {
        // A non-async delegate is only eligible for Rule 2 (Rule 1 requires an async return), so
        // the named delegate and its Func<> equivalent should both get exactly the same fixed-value
        // overload and neither should get a sync Func<> overload (there is no async to drop).
        var named = EmitFor("""
            using MethodOverloadGenerator;
            public delegate int GetCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(GetCapacity getCapacity) => true;
            }
            """);

        var equivalent = EmitFor("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int> getCapacity) => true;
            }
            """);

        Assert.Contains("bool Process(int getCapacityValue)", named);
        Assert.Contains("bool Process(int getCapacityValue)", equivalent);
        Assert.DoesNotContain("Func<int> getCapacity)", named);
        Assert.DoesNotContain("Func<int> getCapacity)", equivalent);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate returning void with no parameters — no overloads possible
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_VoidNoParams_MethodLevelAttribute_ReportsWarning()
    {
        // Not async (Rule 1), no return value (Rule 2), no inputs to drop (Rule 3) — no rule
        // can ever apply to this delegate, regardless of configuration.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public delegate void DoNothing();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(DoNothing action) { }
            }
            """);

        Assert.Contains(result.Warnings, d => d.Id == "MOG001");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void NamedDelegate_VoidNoParams_ClassLevelAttribute_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public delegate void DoNothing();
            [MethodOverloadGeneratorAttribute]
            public partial class AnimalShelter
            {
                public void Process(DoNothing action) { }
            }
            """);

        Assert.Empty(result.Warnings);
        Assert.Empty(result.Errors);
        Assert.Empty(result.GeneratedSources);
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate with multiple input parameters (Rule 3)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_WithMultipleInputParams_GetsTrailingParamOverloads()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<bool> CanAdmit(int legs, bool canFly);
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(CanAdmit canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", source); // k=1, stays async
        Assert.Contains("Func<Task<bool>> canAdmit", source);      // k=0, stays async
    }

    [Fact]
    public void NamedDelegate_WithMultipleInputParams_AllSyncVariantsGenerated()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<bool> CanAdmit(int legs, bool canFly);
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(CanAdmit canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, bool> canAdmit", source); // k=1, also converted to sync
        Assert.Contains("Func<bool> canAdmit", source);      // k=0, also converted to sync
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate on an extension method (Rule 4)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_AsExtensionMethodParam_Rule1AndRule4BothApply()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public interface ICarnivore {}
            public interface IPrey {}
            public delegate Task<IPrey> HuntAsync();
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Hunt(this ICarnivore animal, HuntAsync hunt) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<IPrey> hunt", source);          // Rule 1 — sync
        Assert.Contains("this Task<TCarnivore> animal", source); // Rule 4 — Task<T> receiver
    }

    // -----------------------------------------------------------------------------------------
    // Named delegate used at class level and constructor level
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NamedDelegate_ClassLevelAttribute_GetsOverloads()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchCapacity();
            [MethodOverloadGeneratorAttribute]
            public partial class AnimalShelter
            {
                public Task<bool> Admit(FetchCapacity fetchCapacity) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int> fetchCapacity", source);
    }

    [Fact]
    public void NamedDelegate_ConstructorParam_GetsOverloads()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public delegate Task<int> FetchCapacity();
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(FetchCapacity fetchCapacity) { }
            }
            """);

        Assert.Contains("Func<int> fetchCapacity", source);
        Assert.Contains(": this(", source); // constructors forward via an initializer, not "=> AnimalShelter(...)"
    }

    // -----------------------------------------------------------------------------------------
    // Spec examples — HuntAsync / RelocateAsync
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void HuntAsyncDelegate_Teach_GetsSyncAndFixedValueOverloads()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public class Dog {}
            public delegate Task<bool> HuntAsync();
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static Task<bool> Teach(this Dog dog, HuntAsync hunt) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<bool> hunt", source); // Rule 1 — sync
        Assert.Contains("bool huntValue", source);  // Rule 2 — fixed value
    }

    [Fact]
    public void RelocateAsyncDelegate_Migrate_GetsSyncOverload()
    {
        var source = EmitFor("""
            using MethodOverloadGenerator;
            public interface IBird {}
            public class Location {}
            public delegate Task RelocateAsync(Location destination);
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static Task Migrate(this IBird bird, RelocateAsync relocate) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Action<Location> relocate", source); // Rule 1 — void async becomes Action
    }
}
