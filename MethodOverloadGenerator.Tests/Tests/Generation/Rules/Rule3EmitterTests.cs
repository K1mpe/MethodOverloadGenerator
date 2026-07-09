using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Generation.Rules;

public class Rule3EmitterTests
{
    private readonly Rule3Emitter _sut = new();

    // Async delegates now produce two contexts per k (async-drop and fully-sync); every existing
    // test here exercises the async-drop form, so that's the default. Tests for the new
    // fully-sync dimension pass preserveAsync: false explicitly.
    private string EmitAtK(MasterContext ctx, int k, bool preserveAsync = true)
    {
        var candidates = ctx.Rule3Contexts!.Where(c => c.TargetInputCount == k).ToList();
        var single = candidates.Count == 1 ? candidates[0] : candidates.Single(c => c.PreserveAsync == preserveAsync);
        var result = _sut.Emit([single], ctx.Declaration);
        Assert.Single(result);
        return result[0];
    }

    private IReadOnlyList<string> EmitAll(MasterContext ctx)
        => _sut.Emit(ctx.Rule3Contexts!, ctx.Declaration);

    // -----------------------------------------------------------------------------------------
    // Async drop — parameter types
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncDrop_ParamType_DropsLastInputType_AtK_N_Minus_1()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Func<int, Task<bool>> func", output);
    }

    [Fact]
    public void AsyncDrop_ParamType_IsZeroInputAsync_AtKEqualsZero()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Func<Task<bool>> func", output);
    }

    [Fact]
    public void AsyncDrop_ParamType_UsesValueTask_ForValueTaskDelegate()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<bool> Process(Func<int, string, ValueTask<bool>> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, ValueTask<bool>> func", EmitAtK(ctx, 1));
        Assert.Contains("Func<ValueTask<bool>> func", EmitAtK(ctx, 0));
    }

    [Fact]
    public void AsyncDrop_ParamType_UsesTaskVoid_ForVoidReturn()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, Task> func", EmitAtK(ctx, 1));
        Assert.Contains("Func<Task> func", EmitAtK(ctx, 0));
    }

    [Fact]
    public void AsyncDrop_ParamType_UsesValueTaskVoid_ForValueTaskVoidReturn()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask Process(Func<int, string, ValueTask> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, ValueTask> func", EmitAtK(ctx, 1));
        Assert.Contains("Func<ValueTask> func", EmitAtK(ctx, 0));
    }

    [Fact]
    public void AsyncDrop_ParamType_PreservesRemainingInputs_ForThreeInputDelegate()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, double, Task<int>> func", EmitAtK(ctx, 2));
        Assert.Contains("Func<int, Task<int>> func",         EmitAtK(ctx, 1));
        Assert.Contains("Func<Task<int>> func",              EmitAtK(ctx, 0));
    }

    // -----------------------------------------------------------------------------------------
    // Single-input delegates — k=0 is the only reduction
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SingleInput_AsyncDrop_ParamType_IsZeroInputAsync()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Func<Task<bool>> func", output);
    }

    [Fact]
    public void SingleInput_WrapExpr_LambdaIgnoresSingleArg()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("(a) => func()", output);
    }

    [Fact]
    public void SingleInput_NonAsync_ReducedParamType_IsZeroInputSync()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int, bool> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Func<bool> func", output);
        Assert.Contains("(a) => func()", output);
    }

    // -----------------------------------------------------------------------------------------
    // Non-async delegates — plain-reduced parameter types
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonAsync_ReducedParamType_IsFuncWithKInputs_ForSyncValueReturn()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int, string, bool> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, bool> func", EmitAtK(ctx, 1));
        Assert.Contains("Func<bool> func",       EmitAtK(ctx, 0));
    }

    [Fact]
    public void NonAsync_ReducedParamType_IsActionWithKInputs_ForSyncVoidDelegate()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public void Process(Action<int, string> action) {}
            }
            """);

        var k1 = EmitAtK(ctx, 1);
        var k0 = EmitAtK(ctx, 0);

        Assert.Contains("Action<int> action", k1);
        Assert.Contains("Action action",      k0);
        Assert.DoesNotContain("Action<",      k0);
    }

    // -----------------------------------------------------------------------------------------
    // Fully-sync form — an async delegate can ALSO drop trailing parameters AND lose its async
    // wrapper in the same overload, not just one or the other. This is the combination that
    // was previously missing: "fewer parameters" and "sync method" are independent dimensions
    // and must both be reachable together.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FullySync_ReducedParamType_DropsInputsAndAsyncWrapper()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("Func<int, double, int> func", EmitAtK(ctx, 2, preserveAsync: false));
        Assert.Contains("Func<int, int> func",         EmitAtK(ctx, 1, preserveAsync: false));
        Assert.Contains("Func<int> func",              EmitAtK(ctx, 0, preserveAsync: false));
    }

    [Fact]
    public void FullySync_WrapExpr_WrapsReducedCallInTaskFromResult()
    {
        var output = EmitAtK(k: 1, preserveAsync: false, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("(a, b) => Task.FromResult(func(a))", output);
    }

    [Fact]
    public void FullySync_WrapExpr_UsesNewValueTask_ForValueTaskDelegate()
    {
        var output = EmitAtK(k: 1, preserveAsync: false, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<bool> Process(Func<int, string, ValueTask<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("(a, b) => new ValueTask<bool>(func(a))", output);
    }

    [Fact]
    public void FullySync_WrapExpr_ReturnsTaskCompletedTask_ForVoidReturningDelegate()
    {
        var output = EmitAtK(k: 1, preserveAsync: false, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Action<int> func", output);
        Assert.Contains("(a, b) => { func(a); return Task.CompletedTask; }", output);
    }

    [Fact]
    public void FullySync_Signature_UsesActionType_ForVoidReturningDelegate()
    {
        var output = EmitAtK(k: 0, preserveAsync: false, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, Task> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Action func", output);
        Assert.Contains("(a) => { func(); return Task.CompletedTask; }", output);
    }

    [Fact]
    public void FullySync_Body_CallsOriginalMethod()
    {
        var output = EmitAtK(k: 1, preserveAsync: false, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> FetchDog(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("=> FetchDog(", output);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — full-arity lambda forwarding k args (both async and non-async)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_FullArityLambdaForwardsFirstKArgs()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("(a, b) => func(a)", output);
    }

    [Fact]
    public void WrapExpr_FullArityLambdaWithZeroArgs_AtKEqualsZero()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("(a, b) => func()", output);
    }

    [Fact]
    public void WrapExpr_LambdaHasThreeParams_ForThreeInputDelegate()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("(a, b, c) => func(a, b)", EmitAtK(ctx, 2));
        Assert.Contains("(a, b, c) => func(a)",    EmitAtK(ctx, 1));
        Assert.Contains("(a, b, c) => func()",     EmitAtK(ctx, 0));
    }

    [Fact]
    public void WrapExpr_NonAsync_FullArityLambdaForwardsFirstKArgs()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int, string, bool> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("(a, b) => func(a)", EmitAtK(ctx, 1));
        Assert.Contains("(a, b) => func()",  EmitAtK(ctx, 0));
    }

    // -----------------------------------------------------------------------------------------
    // Method signature
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Signature_UsesMethodReturnType()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Task<bool> Process(", output);
    }

    [Fact]
    public void Signature_UsesAccessModifier()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                internal Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("\n    internal ", output);
    }

    [Fact]
    public void Signature_IncludesStaticKeyword_ForStaticMethod()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("public static ", output);
    }

    [Fact]
    public void Signature_NoStaticKeyword_ForInstanceMethod()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.DoesNotContain("static", output);
    }

    [Fact]
    public void Signature_PreservesOtherParametersAroundDelegate()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(string name, Func<int, string, Task<bool>> func, int cap) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("string name,", output);
        Assert.Contains(", int cap",    output);
    }

    [Fact]
    public void Signature_AddsThisPrefix_ForExtensionMethod()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<bool> Process(this string shelter, Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("this string shelter", output);
    }

    // -----------------------------------------------------------------------------------------
    // Call-site forwarding
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Body_CallsOriginalMethod()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> FetchDog(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("=> FetchDog(", output);
    }

    [Fact]
    public void Body_ForwardsOtherArgsByName()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(string name, Func<int, string, Task<bool>> func, int cap) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Process(name,", output);
        Assert.Contains(", cap)",        output);
    }

    [Fact]
    public void Body_ForwardsThisParamByName_ForExtensionMethod()
    {
        var output = EmitAtK(k: 1, ctx: TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<bool> Process(this string shelter, Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("Process(shelter,", output);
    }

    // -----------------------------------------------------------------------------------------
    // Count and ordering across multiple contexts
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void EachContext_EmitsExactlyOneOverload()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> func) => throw new NotImplementedException();
            }
            """);

        // 2 k-values (k=1, k=0) × 2 forms each (async-drop, fully-sync) = 4
        Assert.Equal(4, EmitAll(ctx).Count);
    }

    [Fact]
    public void AsyncDropsOrderedFromMostToFewestInputs()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        var result = EmitAll(ctx);

        // 3 k-values × 2 forms each (async-drop, fully-sync) = 6
        Assert.Equal(6, result.Count);

        var asyncDrops = result.Where(s => s.Contains("Task<int>>")).ToList();
        Assert.Equal(3, asyncDrops.Count);
        Assert.Contains("Func<int, double, Task<int>>", asyncDrops[0]); // k=2
        Assert.Contains("Func<int, Task<int>>",         asyncDrops[1]); // k=1
        Assert.Contains("Func<Task<int>>",              asyncDrops[2]); // k=0
    }

    [Fact]
    public void MultipleContexts_EmitOneOverloadPerContext()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, string, Task<bool>> funcA, Func<double, long, Task<string>> funcB) => throw new NotImplementedException();
            }
            """);

        var result = EmitAll(ctx);

        // 2 delegates × 2 k-values × 2 forms each = 8
        Assert.Equal(8, result.Count);
        Assert.Contains(result, s => s.Contains("Func<int, Task<bool>>"));
        Assert.Contains(result, s => s.Contains("Func<double, Task<string>>"));
    }

    // -----------------------------------------------------------------------------------------
    // Generic method — the original method's own type parameter and constraint must be
    // reproduced on the generated overload, since it forwards to the original method by name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GenericMethod_PreservesTypeParameterAndConstraint()
    {
        var output = EmitAtK(k: 0, ctx: TestContextHelper.CreateMasterContext("""
            public interface ICarnivore {}
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Feed<T>(T animal, Func<int, Task<string>> fetchFood) where T : ICarnivore => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<bool> Feed<T>(T animal, Func<Task<string>> fetchFood) where T : ICarnivore => Feed(animal, (a) => fetchFood());
            """);
    }

    // -----------------------------------------------------------------------------------------
    // IntelliSense/overload-resolution priority — one point lost per dropped input, plus one
    // extra point whenever the reduced form also converts the delegate to sync: at the same k,
    // the fully-sync form ranks strictly below the async-drop form, since it gives up strictly
    // more (an originally-async delegate that becomes sync loses more than one that stays async).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Priority_DecreasesWithEachDroppedInput_ForAsyncDropForm()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        // N=3; k=2 drops 1 input, stays async → -(1+1) = -2; k=0 drops all 3, stays async → -(1+3) = -4
        Assert.Contains("[OverloadResolutionPriority(-2)]", EmitAtK(ctx, 2, preserveAsync: true));
        Assert.Contains("[OverloadResolutionPriority(-4)]", EmitAtK(ctx, 0, preserveAsync: true));
    }

    [Fact]
    public void Priority_RanksBelowAsyncDropForm_AtSameK_ForFullySyncForm()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """);

        // k=2 drops 1 input AND async → -(1+1+1) = -3 (below the async-drop form's -2 at the same k)
        // k=0 drops all 3 inputs AND async → -(1+3+1) = -5
        Assert.Contains("[OverloadResolutionPriority(-3)]", EmitAtK(ctx, 2, preserveAsync: false));
        Assert.Contains("[OverloadResolutionPriority(-5)]", EmitAtK(ctx, 0, preserveAsync: false));
    }

    [Fact]
    public void Priority_IsMinusOne_ForNonAsyncDelegate_DroppingOneInput()
    {
        // Non-async delegates only ever produce the "drop k" form (there is no async to keep or
        // shed), so N=2, k=1 → -(1 + (2-1)) = -2
        var ctx = TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int, string, bool> func) => throw new NotImplementedException();
            }
            """);

        Assert.Contains("[OverloadResolutionPriority(-2)]", EmitAtK(ctx, 1));
    }
}
