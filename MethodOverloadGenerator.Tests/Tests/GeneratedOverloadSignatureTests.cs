namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Tests that every generated overload has a signature that matches the original method in
/// all aspects that aren't changed by the overload rule — name, return type, access modifier,
/// static/instance, async, generic type parameters, non-delegate parameters, and
/// nullable annotations.
/// </summary>
public class GeneratedOverloadSignatureTests
{
    // -----------------------------------------------------------------------------------------
    // Method name
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_HasExactSameMethodName_AsOriginal()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> FetchDog(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """);

        Assert.Contains("FetchDog(", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedOverload_DoesNotAppendOrPrependAnythingToMethodName()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> FetchDog(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        // No "Async" suffix, no "Sync" prefix, etc. — every call to the method uses the bare name.
        Assert.DoesNotContain("FetchDogAsync", source);
        Assert.DoesNotContain("SyncFetchDog", source);
        Assert.DoesNotContain("FetchDogSync", source);
    }

    // -----------------------------------------------------------------------------------------
    // Return type
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_HasSameReturnType_AsOriginal()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "public Task<string> Process(Func<string> fetch)");
    }

    [Fact]
    public void GeneratedOverload_Rule4Overload_ReturnTypeWrappedInTask_WhenOriginalIsSync()
    {
        // Rule 4 is the only case where the return type changes — sync return becomes Task<T>
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => new Mouse();
            }
            public sealed class Cat {}
            public class Mouse {}
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "public static async Task<Mouse> GetFood(this Task<Cat> cat)");
    }

    [Fact]
    public void GeneratedOverload_Rule1And2Overloads_ReturnTypeUnchanged()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """);

        // Two overloads (Rule 1 sync + Rule 2 fixed value) — both must keep Task<string>.
        var occurrences = result.SingleGeneratedSource.Split("Task<string> Process(").Length - 1;
        Assert.Equal(2, occurrences);
    }

    // -----------------------------------------------------------------------------------------
    // Access modifier on the generated method
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_PublicOriginalMethod_IsPublic()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("public Task<string> Process(", source);
    }

    [Fact]
    public void GeneratedOverload_InternalOriginalMethod_IsInternal()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                internal Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("internal Task<string> Process(", source);
    }

    [Fact]
    public void GeneratedOverload_PrivateOriginalMethod_IsPrivate()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                private Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("private Task<string> Process(", source);
    }

    [Fact]
    public void GeneratedOverload_ProtectedOriginalMethod_IsProtected()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                protected Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("protected Task<string> Process(", source);
    }

    [Fact]
    public void GeneratedOverload_ProtectedInternalOriginalMethod_IsProtectedInternal()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                protected internal Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("protected internal Task<string> Process(", source);
    }

    // -----------------------------------------------------------------------------------------
    // `static` modifier
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_StaticOriginalMethod_IsStatic()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("public static Task<string> Process(", source);
    }

    [Fact]
    public void GeneratedOverload_InstanceOriginalMethod_IsNotStatic()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.DoesNotContain("static", source);
    }

    [Fact]
    public void GeneratedOverload_ExtensionMethod_IsStatic()
    {
        // Extension methods are always static (a C# requirement on the original declaration too)
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("public static Task<string> Process(this string shelter, ", source);
    }

    // -----------------------------------------------------------------------------------------
    // `async` modifier
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_Rule4_HasAsyncModifier()
    {
        // Rule 4 overloads must await the Task<T>/ValueTask<T> input so they need async
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """).SingleGeneratedSource;

        Assert.Contains("static async Task<IPrey> Feed", source);
    }

    [Fact]
    public void GeneratedOverload_Rule1SyncOverload_DoesNotHaveUnnecessaryAsyncModifier()
    {
        // A sync overload that just wraps with Task.FromResult doesn't need async
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.DoesNotContain("async", source);
    }

    [Fact]
    public void GeneratedOverload_Rule2FixedValueOverload_DoesNotHaveUnnecessaryAsyncModifier()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.DoesNotContain("async", source);
    }

    // -----------------------------------------------------------------------------------------
    // Non-delegate parameters are preserved unchanged
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_PreservesNonDelegateParams_InOriginalOrder()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetch, int capacity) => Task.FromResult(name);
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "Process(string name, Func<string> fetch, int capacity)");
    }

    [Fact]
    public void GeneratedOverload_PreservesNonDelegateParamNames()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string shelterName, Func<Task<string>> fetch) => Task.FromResult(shelterName);
            }
            """).SingleGeneratedSource;

        Assert.Contains("string shelterName", source);
    }

    [Fact]
    public void GeneratedOverload_PreservesNonDelegateParamTypes()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(TimeSpan delay, Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("TimeSpan delay", source);
    }

    [Fact]
    public void GeneratedOverload_NonDelegateParamBeforeDelegate_PreservedAtSamePosition()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(string name, Func<Task<string>> fetch) => Task.FromResult(name);
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "Process(string name, Func<string> fetch)");
    }

    [Fact]
    public void GeneratedOverload_NonDelegateParamAfterDelegate_PreservedAtSamePosition()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch, int capacity) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "Process(Func<string> fetch, int capacity)");
    }

    [Fact]
    public void GeneratedOverload_NonDelegateParamWithDefaultValue_DefaultValuePreserved()
        // Not yet implemented — MethodParameter only captures Type and Name, so this currently fails.
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch, int capacity = 10) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "Process(Func<string> fetch, int capacity = 10)");
    }

    [Fact]
    public void GeneratedOverload_ParamsKeyword_OnNonDelegateParam_Preserved()
        // Not yet implemented — MethodParameter never inspects IParameterSymbol.IsParams, so this currently fails.
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch, params int[] extra) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        TestContextHelper.NormalisedContains(source, "Process(Func<string> fetch, params int[] extra)");
    }

    // -----------------------------------------------------------------------------------------
    // `this` parameter on extension methods
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_ExtensionMethod_ThisParamType_MatchesOriginal_ForRule1And2()
    {
        // Only Rule 4 changes the this-param type to Task<T>/ValueTask<T>
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class MyService
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static Task<string> Process(this string shelter, Func<Task<string>> fetch) => Task.FromResult(shelter);
            }
            """).SingleGeneratedSource;

        Assert.Contains("this string shelter", source);
        Assert.DoesNotContain("this Task<string> shelter", source);
    }

    [Fact]
    public void GeneratedOverload_Rule4_ThisParamName_PreservedFromOriginal()
    {
        // A sealed receiver keeps Task<Cat> literal (an interface/non-sealed-class receiver
        // instead synthesizes a covariant type parameter — see Rule4EmitterTests) — either way,
        // the parameter's own *name* must survive unchanged.
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat predator) => new Mouse();
            }
            public sealed class Cat {}
            public class Mouse {}
            """).SingleGeneratedSource;

        Assert.Contains("this Task<Cat> predator", source);
    }

    // -----------------------------------------------------------------------------------------
    // Generic type parameters on the method
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_GenericMethod_TypeParametersPreserved()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public interface ICarnivore {}
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Feed<T>(T animal, Func<Task<string>> fetchFood) where T : ICarnivore => throw new NotImplementedException();
            }
            """).SingleGeneratedSource;

        Assert.Contains("Feed<T>(T animal, Func<string> fetchFood)", source);
    }

    [Fact]
    public void GeneratedOverload_GenericMethod_TypeConstraintsPreserved()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public interface ICarnivore {}
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Feed<T>(T animal, Func<Task<string>> fetchFood) where T : ICarnivore => throw new NotImplementedException();
            }
            """).SingleGeneratedSource;

        Assert.Contains("where T : ICarnivore", source);
    }

    [Fact]
    public void GeneratedOverload_GenericMethod_NoSpuriousTypeParametersAdded()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.DoesNotContain("Process<", source);
        Assert.DoesNotContain("where ", source);
    }

    // -----------------------------------------------------------------------------------------
    // Nullable annotations
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverload_NullableReferenceTypeDelegate_FixedValueParam_IsNullable()
        // Not yet implemented — DeclarationContextBuilder uses SymbolDisplayFormat.MinimallyQualifiedFormat
        // without IncludeNullableReferenceTypeModifier, so this currently fails.
    {
        var source = TestHelper.RunGenerator("""
            #nullable enable
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string?> Process(Func<Task<string?>> fetch) => Task.FromResult<string?>(null);
            }
            """).SingleGeneratedSource;

        Assert.Contains("string? fetchValue", source);
    }

    [Fact]
    public void GeneratedOverload_NonNullableDelegate_FixedValueParam_IsNotNullable()
    {
        var source = TestHelper.RunGenerator("""
            #nullable enable
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("string fetchValue", source);
        Assert.DoesNotContain("string? fetchValue", source);
    }

    [Fact]
    public void GeneratedOverload_NullableAnnotations_OnNonDelegateParams_Preserved()
        // Not yet implemented — same root cause as the fixed-value case above, so this currently fails.
    {
        var source = TestHelper.RunGenerator("""
            #nullable enable
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch, string? name) => Task.FromResult("Rex");
            }
            """).SingleGeneratedSource;

        Assert.Contains("string? name", source);
    }

    // -----------------------------------------------------------------------------------------
    // No duplicate overloads
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverloads_AreAllDistinct_NoTwoWithIdenticalSignatures()
    {
        var source = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2, Func<Task<bool>> p3) => Task.CompletedTask;
            }
            """).SingleGeneratedSource;

        // Each overload starts with its own [OverloadResolutionPriority(...)] attribute line,
        // immediately followed by its signature up to the "=>" — extract and dedupe those.
        var signatures = source
            .Split("[OverloadResolutionPriority(")
            .Skip(1)
            .Select(block => block[(block.IndexOf(')') + 1)..block.IndexOf("=>")].Trim())
            .ToList();

        // 3 Rule 1 (one per param) + 3 Rule 2 (one per param) + 20 Rule 5 combinatorial = 26.
        Assert.Equal(26, signatures.Count);
        Assert.Equal(signatures.Count, signatures.Distinct().Count());
    }

    [Fact]
    public void GeneratedOverloads_DoNotConflictWithExistingMethodsOnClass()
        // Not yet implemented — the generator never looks at the containing type's other members,
        // so this currently fails with CS0111 (duplicate member).
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<string> Process(Func<Task<string>> fetch) => Task.FromResult("Rex");

                // Hand-written overload that collides with the Rule 1 sync overload the generator
                // would otherwise produce for the method above.
                public Task<string> Process(Func<string> fetch) => Task.FromResult(fetch());
            }
            """);

        Assert.Empty(result.Errors);
    }
}
