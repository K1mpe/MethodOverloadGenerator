using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Generation.Rules;

public class Rule2EmitterTests
{
    private readonly Rule2Emitter _sut = new();

    private string Suffix => GeneratorConstants.ValueParamSuffix;
    private string EmitOne(MasterContext ctx)
    {
        var result = _sut.Emit(ctx.Rule2Contexts!, ctx.Declaration);
        Assert.Single(result);
        return result[0];
    }

    // -----------------------------------------------------------------------------------------
    // Value parameter type — always the delegate's return type
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ParamType_IsDelegateReturnType_ForSyncDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public string Process(string getDog{Suffix}) => Process(() => getDog{Suffix});
            """);
    }

    [Fact]
    public void ParamType_IsDelegateReturnType_ForAsyncDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<string> Process(string fetchDog{Suffix}) => Process(() => Task.FromResult(fetchDog{Suffix}));
            """);
    }

    [Fact]
    public void ParamType_IsDelegateReturnType_ForValueTaskDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public ValueTask<string> Process(string fetchDog{Suffix}) => Process(() => new ValueTask<string>(fetchDog{Suffix}));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — sync delegate (no async, no input types)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_IsConstantLambda_ForSyncDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public string Process(string getDog{Suffix}) => Process(() => getDog{Suffix});
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — async Task
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_UsesTaskFromResult_ForFuncTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<string> Process(string fetchDog{Suffix}) => Process(() => Task.FromResult(fetchDog{Suffix}));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — async ValueTask
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_UsesNewValueTaskT_ForFuncValueTaskT()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public ValueTask<string> Process(Func<ValueTask<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public ValueTask<string> Process(string fetchDog{Suffix}) => Process(() => new ValueTask<string>(fetchDog{Suffix}));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — delegates with input types (inputs are accepted and ignored)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void WrapExpr_IncludesTypedLambdaParam_ForSyncDelegateWithOneInput()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<int, string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public string Process(string getDog{Suffix}) => Process((int p0) => getDog{Suffix});
            """);
    }

    [Fact]
    public void WrapExpr_IncludesTypedLambdaParams_ForAsyncDelegateWithMultipleInputs()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<bool> Process(bool fetchDog{Suffix}) => Process((int p0, string p1) => Task.FromResult(fetchDog{Suffix}));
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Method signature
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Signature_UsesMethodReturnType()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<Task<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<bool> Process(string fetchDog{Suffix}) => Process(() => Task.FromResult(fetchDog{Suffix}));
            """);
    }

    [Fact]
    public void Signature_PreservesOtherParametersAroundDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetchDog, int cap) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<string> Process(string name, string fetchDog{Suffix}, int cap) => Process(name, () => Task.FromResult(fetchDog{Suffix}), cap);
            """);
    }

    [Fact]
    public void Signature_AddsThisPrefix_ForExtensionMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public static Task<string> Process(this string shelter, string fetchDog{Suffix}) => Process(shelter, () => Task.FromResult(fetchDog{Suffix}));
            """);
    }

    [Fact]
    public void Signature_IncludesStaticKeyword_ForStaticMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static string Process(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public static string Process(string getDog{Suffix}) => Process(() => getDog{Suffix});
            """);
    }

    [Fact]
    public void Signature_UsesAccessModifier()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                internal string Process(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            internal string Process(string getDog{Suffix}) => Process(() => getDog{Suffix});
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
                public string Fetch(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public string Fetch(string getDog{Suffix}) => Fetch(() => getDog{Suffix});
            """);
    }

    [Fact]
    public void Body_ForwardsAllOtherArgs()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetchDog, int cap) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<string> Process(string name, string fetchDog{Suffix}, int cap) => Process(name, () => Task.FromResult(fetchDog{Suffix}), cap);
            """);
    }

    [Fact]
    public void Body_ForwardsThisParamByName_ForExtensionMethod()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetchDog) => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public static Task<string> Process(this string shelter, string fetchDog{Suffix}) => Process(shelter, () => Task.FromResult(fetchDog{Suffix}));
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
                public Task<int> Process(Func<string> getDog, Func<Task<bool>> getCat) => throw new NotImplementedException();
            }
            """);

        var result = _sut.Emit(ctx.Rule2Contexts!, ctx.Declaration);

        Assert.Equal(2, result.Count);
        TestContextHelper.NormalisedContains(result[0], $"""
            public Task<int> Process(string getDog{Suffix}, Func<Task<bool>> getCat) => Process(() => getDog{Suffix}, getCat);
            """);
        TestContextHelper.NormalisedContains(result[1], $"""
            public Task<int> Process(Func<string> getDog, bool getCat{Suffix}) => Process(getDog, () => Task.FromResult(getCat{Suffix}));
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
                public Task<bool> Feed<T>(T animal, Func<string> getFood) where T : ICarnivore => throw new NotImplementedException();
            }
            """));

        TestContextHelper.NormalisedContains(output, $"""
            public Task<bool> Feed<T>(T animal, string getFood{Suffix}) where T : ICarnivore => Feed(animal, () => getFood{Suffix});
            """);
    }

    // -----------------------------------------------------------------------------------------
    // IntelliSense/overload-resolution priority — Rule 2 eliminates the delegate entirely, which
    // is worse than even a zero-input delegate, so its priority is -(N + 2) where N is the
    // delegate's own input count (one point per lost input, plus one extra for losing the
    // delegate abstraction itself, plus the baseline -1 every generated overload starts at).
    // These two delegates are already non-async (plain Func<string>/Func<...,bool>), so there is
    // no additional "lost async" penalty here — see the next test for that case.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Priority_IsMinusTwo_ForZeroInputDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public string Process(Func<string> getDog) => throw new NotImplementedException();
            }
            """));

        Assert.Contains("[OverloadResolutionPriority(-2)]", output);
    }

    [Fact]
    public void Priority_AccountsForInputCount_ForMultiInputDelegate()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public bool Process(Func<int, double, bool> func) => throw new NotImplementedException();
            }
            """));

        // N=2 inputs → -(2 + 2) = -4
        Assert.Contains("[OverloadResolutionPriority(-4)]", output);
    }

    [Fact]
    public void Priority_HasExtraPenalty_WhenDelegateWasAsync()
    {
        var output = EmitOne(TestContextHelper.CreateMasterContext("""
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> getDog) => throw new NotImplementedException();
            }
            """));

        // N=0 inputs, but was async → -(0 + 2 + 1) = -3, one worse than the non-async equivalent
        Assert.Contains("[OverloadResolutionPriority(-3)]", output);
    }
}
