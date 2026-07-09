using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Generation.Rules;

public class Rule1EmitterTests
{
    private readonly Rule1Emitter _sut = new();

    private string EmitOne(MasterContext ctx)
    {
        var result = _sut.Emit(ctx.Rule1Contexts!, ctx.Declaration);
        Assert.Single(result);
        return result[0];
    }

    // -----------------------------------------------------------------------------------------
    // Sync parameter type — no input types
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncType_IsFuncT_ForFuncTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void SyncType_IsFuncT_ForFuncValueTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public ValueTask<string> Process(Func<string> fetch) => Process(() => new ValueTask<string>(fetch()));
            """);
    }

    [Fact]
    public void SyncType_IsAction_ForFuncTask()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task Process(Action action) => Process(() => { action(); return Task.CompletedTask; });
            """);
    }

    [Fact]
    public void SyncType_IsAction_ForFuncValueTask()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask Process(Func<ValueTask> action) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public ValueTask Process(Action action) => Process(() => { action(); return default; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Sync parameter type — with input types
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncType_IsFuncInputsT_ForFuncInputsTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<int, Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<int, string> fetch) => Process((int p0) => Task.FromResult(fetch(p0)));
            """);
    }

    [Fact]
    public void SyncType_IsActionInputs_ForFuncInputsTask()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, string, Task> cb) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task Process(Action<int, string> cb) => Process((int p0, string p1) => { cb(p0, p1); return Task.CompletedTask; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — Task variants
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_UsesTaskFromResult_ForFuncTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void WrapExpr_UsesTaskCompletedTask_ForFuncTask()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task Process(Action action) => Process(() => { action(); return Task.CompletedTask; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — ValueTask variants
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_UsesNewValueTaskT_ForFuncValueTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public ValueTask<string> Process(Func<string> fetch) => Process(() => new ValueTask<string>(fetch()));
            """);
    }

    [Fact]
    public void WrapExpr_UsesReturnDefault_ForFuncValueTask()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask Process(Func<ValueTask> action) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public ValueTask Process(Action action) => Process(() => { action(); return default; });
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — lambda input parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_IncludesTypedLambdaParam_ForOneInputType()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<int, Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<int, string> fetch) => Process((int p0) => Task.FromResult(fetch(p0)));
            """);
    }

    [Fact]
    public void WrapExpr_IncludesTypedLambdaParams_ForMultipleInputTypes()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<bool> Process(Func<int, string, bool> fetch) => Process((int p0, string p1) => Task.FromResult(fetch(p0, p1)));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Method signature — modifiers and return type
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Signature_UsesMethodReturnType()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void Signature_UsesAccessModifier()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                internal Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            internal Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void Signature_IncludesStaticKeyword_ForStaticMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public static Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void Signature_NoStaticKeyword_ForInstanceMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<string> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Method signature — parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Signature_PreservesOtherParametersBeforeDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(string name, Func<string> fetch) => Process(name, () => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void Signature_PreservesOtherParametersAfterDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch, int capacity) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(Func<string> fetch, int capacity) => Process(() => Task.FromResult(fetch()), capacity);
            """);
    }

    [Fact]
    public void Signature_AddsThisPrefix_ForExtensionMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public static Task<string> Process(this string shelter, Func<string> fetch) => Process(shelter, () => Task.FromResult(fetch()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Call-site forwarding
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Body_CallsOriginalMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> FetchDog(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> FetchDog(Func<string> fetch) => FetchDog(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void Body_ForwardsAllOtherArgs()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetch, int capacity) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<string> Process(string name, Func<string> fetch, int capacity) => Process(name, () => Task.FromResult(fetch()), capacity);
            """);
    }

    [Fact]
    public void Body_ForwardsThisParamByName_ForExtensionMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public static Task<string> Process(this string shelter, Func<string> fetch) => Process(shelter, () => Task.FromResult(fetch()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Multiple contexts
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MultipleContexts_ProduceMultipleMethods()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<string>> fetchA, Func<Task<bool>> fetchB) => throw new NotImplementedException();
            }
            """);

        var result = _sut.Emit(ctx.Rule1Contexts!, ctx.Declaration);

        Assert.Equal(2, result.Count);
        TestContextHelper.NormalisedContains(result[0], """
            public Task<int> Process(Func<string> fetchA, Func<Task<bool>> fetchB) => Process(() => Task.FromResult(fetchA()), fetchB);
            """);
        TestContextHelper.NormalisedContains(result[1], """
            public Task<int> Process(Func<Task<string>> fetchA, Func<bool> fetchB) => Process(fetchA, () => Task.FromResult(fetchB()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Generic method — the original method's own type parameter and constraint must be
    // reproduced on the generated overload, since it forwards to the original method by name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GenericMethod_PreservesTypeParameterAndConstraint()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public interface ICarnivore {}
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Feed<T>(T animal, Func<Task<string>> fetchFood) where T : ICarnivore => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, """
            public Task<bool> Feed<T>(T animal, Func<string> fetchFood) where T : ICarnivore => Feed(animal, () => Task.FromResult(fetchFood()));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // IntelliSense/overload-resolution priority — Rule 1 keeps the delegate's full arity but
    // always turns it synchronous, so it always incurs the one-point "lost async" penalty
    // (regardless of how many inputs the delegate has) and ranks below an overload that keeps
    // the original delegate async in every way (e.g. Rule 4's receiver-only overloads).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Priority_IsMinusTwo_RegardlessOfInputCount()
    {
        var zeroInputs = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => throw new NotImplementedException();
            }
            """));

        var threeInputs = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<int, double, bool, Task<int>> func) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("[OverloadResolutionPriority(-2)]", zeroInputs);
        Assert.Contains("[OverloadResolutionPriority(-2)]", threeInputs);
    }
}
