using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Generation.Rules;

public class Rule5EmitterTests
{
    private readonly Rule5Emitter _sut = new();

    private IReadOnlyList<string> Emit(MasterContext ctx)
        => _sut.Emit(ctx.Rule5Context!, ctx.Declaration, ctx.AllowedRules, ctx.Rule4Context);

    // -----------------------------------------------------------------------------------------
    // Rule 1 × Rule 1 — both delegate params get sync variant (internal access modifier)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void BothSync_InternalModifier()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable, trailingParameterOverloads: RuleOverride.Disable)]
                internal Task Process(Func<Task<string>> p1, Func<Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        Assert.Single(result);
        TestContextHelper.NormalisedContains(result, """
            internal Task Process(Func<string> p1, Func<int> p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 1 × Rule 2 — first param sync, second param value (public static method)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncThenValue_AsStaticMethod()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task Process(Func<Task<string>> p1, Func<Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static Task Process(Func<string> p1, int p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 2 × Rule 1 — first param value, second param sync (with non-delegate params)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ValueThenSync_WithNonDelegateParams()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public Task Process(string name, Func<Task<string>> p1, Func<Task<int>> p2, int cap) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task Process(string name, string p1, Func<int> p2, int cap) => Process(name, () => Task.FromResult(p1), () => Task.FromResult(p2()), cap);
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 2 × Rule 2 — both params value (extension method with this prefix)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void BothValue_AsExtensionMethod()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, trailingParameterOverloads: RuleOverride.Disable, taskReceiverOverloads: RuleOverride.Disable)]
                public static Task Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        Assert.Single(result);
        TestContextHelper.NormalisedContains(result, """
            public static Task Process(this string shelter, string p1, int p2) => Process(shelter, () => Task.FromResult(p1), () => Task.FromResult(p2));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 3 (drop last input) × Rule 1 — different method name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void DropLastInput_ThenSync_DifferentMethodName()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task FetchAll(Func<int, string, Task<string>> p1, Func<Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task FetchAll(Func<int, Task<string>> p1, Func<int> p2) => FetchAll((a, b) => p1(a), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 3 (drop all inputs) × Rule 2
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void DropAllInputs_ThenValue()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task<string>> p1, Func<Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task Process(Func<Task<string>> p1, int p2) => Process((a, b) => p1(), () => Task.FromResult(p2));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 1 × Rule 3 (drop single input)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Sync_ThenDropSingleInput()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<string>> p1, Func<int, Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task Process(Func<string> p1, Func<Task<int>> p2) => Process(() => Task.FromResult(p1()), (a) => p2());
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 2 × Rule 3 (drop last of two inputs)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Value_ThenDropLastInput()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<string>> p1, Func<int, string, Task<int>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task Process(string p1, Func<int, Task<int>> p2) => Process(() => Task.FromResult(p1), (a, b) => p2(a));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (Task<T> receiver) × Rule 1 × Rule 1
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TaskReceiver_BothSync()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process(this Task<string> shelter, Func<string> p1, Func<int> p2) => await (await shelter).Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (Task<T> receiver) × Rule 1 × Rule 2
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TaskReceiver_SyncThenValue()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process(this Task<string> shelter, Func<string> p1, int p2) => await (await shelter).Process(() => Task.FromResult(p1()), () => Task.FromResult(p2));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (ValueTask<T> receiver) × Rule 1 × Rule 1
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ValueTaskReceiver_BothSync()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process(this ValueTask<string> shelter, Func<string> p1, Func<int> p2) => await (await shelter).Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (ValueTask<T> receiver) × Rule 2 × Rule 2
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ValueTaskReceiver_BothValue()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process(this ValueTask<string> shelter, string p1, int p2) => await (await shelter).Process(() => Task.FromResult(p1), () => Task.FromResult(p2));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (Task<T> receiver) × Rule 2 × Rule 1 — the reverse ordering of TaskReceiver_SyncThenValue
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TaskReceiver_ValueThenSync()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process(this Task<string> shelter, string p1, Func<int> p2) => await (await shelter).Process(() => Task.FromResult(p1), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 receiver combination excludes the "receiver-only" substitution — that combination
    // (both delegate parameters left at their original type) is already emitted by Rule4Emitter,
    // so Rule 5 must not duplicate it.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReceiverCombination_ExcludesParameterlessSubstitution()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        Assert.DoesNotContain(result, s =>
            s.Contains("Task<string> shelter") &&
            s.Contains("Func<Task<string>> p1") &&
            s.Contains("Func<Task<int>> p2"));
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 receiver on a non-sealed reference type (interface) — the receiver must use the
    // synthesized generic type parameter (not the interface type itself, since Task<T> is not
    // covariant), the generated method must declare that type parameter, and the call site must
    // infer it rather than repeat it explicitly (matching Rule4Emitter's own behaviour).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReceiverCombination_WithGenericInterfaceReceiver()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public interface IAnimal { }
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this IAnimal shelter, Func<Task<string>> p1, Func<Task<int>> p2) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<string> Process<TAnimal>(this Task<TAnimal> shelter, Func<string> p1, Func<int> p2)
                where TAnimal : IAnimal
                => await (await shelter).Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Three attributed parameters — multiplicative growth (see README "Rule 5" section).
    // Each Func<Task<T>> parameter has 3 variants (original / sync / value); combinations with
    // fewer than two substituted parameters are already covered by Rules 1–2, so Rule 5 emits
    // only the 3^3 - 1 (all-substituted-or-partial) combinations of depth >= 2: 20 in total.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThreeAttributedParams_ProducesExpectedCombinationCountAndContent()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2, Func<Task<bool>> p3) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        Assert.Equal(20, result.Count);

        TestContextHelper.NormalisedContains(result,
            """
            public Task Process(Func<int> p1, Func<string> p2, Func<bool> p3) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()), () => Task.FromResult(p3()));
            """,
            """
            public Task Process(Func<int> p1, string p2, Func<Task<bool>> p3) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2), p3);
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Generic method — the original method's own type parameter and constraint must be
    // reproduced on every combinatorial overload, since it forwards to the original method
    // by name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void CombinatorialOverload_PreservesGenericMethodTypeParameterAndConstraint()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public interface ICarnivore {}
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Feed<T>(T animal, Func<Task<int>> p1, Func<Task<string>> p2) where T : ICarnivore => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task<bool> Feed<T>(T animal, Func<int> p1, Func<string> p2) where T : ICarnivore => Feed(animal, () => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Receiver combination reusing the method's own generic type parameter (as opposed to a
    // synthesized one) — the original constraint must still be reproduced, even though Rule 4
    // itself needs no additional constraint for a reused type parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ReceiverCombination_ReusesGenericMethodTypeParameter_PreservesConstraint()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public interface ICarnivore {}
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<T> Eat<T>(this T animal, Func<Task<int>> p1, Func<Task<string>> p2) where T : ICarnivore => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public static async Task<T> Eat<T>(this Task<T> animal, Func<int> p1, Func<string> p2) where T : ICarnivore => await (await animal).Eat(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // "Fewer parameters" and "sync method" combined on ONE parameter, crossed with a plain sync
    // substitution on ANOTHER parameter. This is the exact shape that previously failed: an
    // async delegate's own per-parameter variant menu only offered "still async, fewer params"
    // or "sync, same params" — never "sync, fewer params" — so no combination existed for a
    // caller that wants a reduced-arity, fully-synchronous delegate for one parameter alongside
    // a synchronous delegate for another.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void CombinesReducedArityAndSyncOnOneParameter_WithSyncOnAnotherParameter()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Generic(MakeCarnivore make, Func<Task<int>> fetchPrey) => throw new NotImplementedException();

                public delegate Task<bool> MakeCarnivore(int legs, bool canFly, bool canSwim, string maleOrFemale);
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result, """
            public Task<bool> Generic(Func<int, bool, bool, bool> make, Func<int> fetchPrey) => Generic((a, b, c, d) => Task.FromResult(make(a, b, c)), () => Task.FromResult(fetchPrey()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // IntelliSense/overload-resolution priority — combinatorial overloads sum the reduction
    // (including the "lost async" penalty) across every substituted parameter, so changing two
    // parameters ranks below changing just one of them the same way.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Priority_SumsLostAsyncPenalty_WhenEveryChangedParameterKeepsFullArity()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        var result = Emit(ctx);

        // p1 sync (1, lost async) + p2 sync (1, lost async) → total 2 → -(1+2) = -3
        TestContextHelper.NormalisedContains(result, """
            [OverloadResolutionPriority(-3)]
            public Task Process(Func<int> p1, Func<string> p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }

    [Fact]
    public void Priority_SumsReductionAcrossSubstitutedParameters()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Generic(MakeCarnivore make, Func<Task<int>> fetchPrey) => throw new NotImplementedException();

                public delegate Task<bool> MakeCarnivore(int legs, bool canFly, bool canSwim, string maleOrFemale);
            }
            """);

        var result = Emit(ctx);

        // make: fully-sync-drop from 4 to 3 inputs, also loses async → (4-3)+1 = 2
        // fetchPrey: sync (0 inputs), also loses async → 0+1 = 1
        // total 3 → -(1+3) = -4
        TestContextHelper.NormalisedContains(result, """
            [OverloadResolutionPriority(-4)]
            public Task<bool> Generic(Func<int, bool, bool, bool> make, Func<int> fetchPrey) => Generic((a, b, c, d) => Task.FromResult(make(a, b, c)), () => Task.FromResult(fetchPrey()));
            """);
    }

    [Fact]
    public void Priority_ForReceiverCombination_SumsReductionAcrossSubstitutedParameters()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public static Task<T> Eat<T>(this T animal, Func<Task<int>> p1, Func<Task<string>> p2) where T : ICarnivore => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            """);

        var result = Emit(ctx);

        // p1 sync (1, lost async) + p2 sync (1, lost async), receiver crossing itself contributes
        // nothing → total 2 → -(1+2) = -3
        TestContextHelper.NormalisedContains(result, """
            [OverloadResolutionPriority(-3)]
            public static async Task<T> Eat<T>(this Task<T> animal, Func<int> p1, Func<string> p2) where T : ICarnivore => await (await animal).Eat(() => Task.FromResult(p1()), () => Task.FromResult(p2()));
            """);
    }
}
