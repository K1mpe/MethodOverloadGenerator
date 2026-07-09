namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Tests that [MethodOverloadGenerator] is correctly applied at the three supported placement
/// levels — parameter, method/constructor, and class — and that the scope of each level is
/// respected.
/// </summary>
public class AttributePlacementTests
{
    private static string Generate(string source) => TestHelper.RunGenerator(source).SingleGeneratedSource;

    // -----------------------------------------------------------------------------------------
    // Parameter level
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ParameterLevel_SingleDelegateParam_GeneratesOverloadsForThatParam()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => fetch();
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetch) => Process(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void ParameterLevel_OnlyAttributedParam_GetsOverloads_OtherParamsUnchanged()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch, Func<Task<string>> other) => fetch();
            }
            """);

        // "other" is a valid (delegate) candidate for overloads, but it wasn't attributed, so
        // parameter-level placement leaves it untouched — it keeps its original delegate type in
        // every generated overload, and never gets a sync/fixed-value overload of its own.
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetch, Func<Task<string>> other) => Process(() => Task.FromResult(fetch()), other);
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(int fetchValue, Func<Task<string>> other) => Process(() => Task.FromResult(fetchValue), other);
            """);
        Assert.DoesNotContain("Func<string> other", source);
        Assert.DoesNotContain("otherValue", source);
    }

    [Fact]
    public void ParameterLevel_MultipleAttributedParams_EachGeneratesItsOwnOverloads()
    {
        // Each parameter-level attribute usage is an independent candidate that produces its own
        // generated file (identified by containing-method + parameter name, so the two usages on
        // this method don't collide), and each is scoped to just its own parameter.
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process(
                    [MethodOverloadGeneratorAttribute] Func<Task<int>> fetchCapacity,
                    [MethodOverloadGeneratorAttribute] Func<Task<string>> fetchName) => fetchCapacity();
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedSources.Count);

        TestContextHelper.NormalisedContains(result.GeneratedSources, """
            public Task<int> Process(Func<int> fetchCapacity, Func<Task<string>> fetchName)
                => Process(() => Task.FromResult(fetchCapacity()), fetchName);
            """);
        TestContextHelper.NormalisedContains(result.GeneratedSources, """
            public Task<int> Process(Func<Task<int>> fetchCapacity, Func<string> fetchName)
                => Process(fetchCapacity, () => Task.FromResult(fetchName()));
            """);
    }

    [Fact]
    public void ParameterLevel_OnNonDelegateParam_ReportsCompileError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] int capacity) => Task.FromResult(capacity);
            }
            """);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MOG005", error.Id);
    }

    // -----------------------------------------------------------------------------------------
    // Method level
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MethodLevel_SingleDelegateParam_EquivalentToAttributingThatParam()
    {
        var methodLevel = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        var parameterLevel = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Equal(Normalise(parameterLevel), Normalise(methodLevel));
    }

    [Fact]
    public void MethodLevel_MultipleDelegateParams_AllParamsTreatedAsAttributed_Combinatorial()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) => fetchCapacity();
            }
            """);

        // Every delegate parameter is treated as attributed at method level, so both individual
        // overloads and the Rule 5 combinatorial overload (both substituted together) appear.
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetchCapacity, Func<Task<string>> fetchName)
                => Process(() => Task.FromResult(fetchCapacity()), fetchName);
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<Task<int>> fetchCapacity, Func<string> fetchName)
                => Process(fetchCapacity, () => Task.FromResult(fetchName()));
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetchCapacity, Func<string> fetchName)
                => Process(() => Task.FromResult(fetchCapacity()), () => Task.FromResult(fetchName()));
            """);
    }

    [Fact]
    public void MethodLevel_MixedDelegateAndNonDelegateParams_OnlyDelegateParamsOverloaded()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch, int count) => fetch();
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(Func<int> fetch, int count) => Process(() => Task.FromResult(fetch()), count);
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> Process(int fetchValue, int count) => Process(() => Task.FromResult(fetchValue), count);
            """);
    }

    [Fact]
    public void MethodLevel_DoesNotAffectOtherMethodsInTheSameClass()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> ProcessA(Func<Task<int>> fetch) => fetch();

                public Task<string> ProcessB(Func<Task<string>> fetch) => fetch();
            }
            """);

        var source = Assert.Single(result.GeneratedSources);
        Assert.Contains("ProcessA", source);
        Assert.DoesNotContain("ProcessB", source);
    }

    [Fact]
    public void MethodLevel_NoDelegateParamsAvailable_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public int Process(int capacity, string name) => capacity;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG001", warning.Id);
    }

    // -----------------------------------------------------------------------------------------
    // Constructor level
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ConstructorLevel_SingleDelegateParam_EquivalentToAttributingThatParam()
    {
        var constructorLevel = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public MyService(Func<Task<int>> fetch) { }
            }
            """);

        var parameterLevel = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public MyService([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) { }
            }
            """);

        Assert.Equal(Normalise(parameterLevel), Normalise(constructorLevel));
    }

    [Fact]
    public void ConstructorLevel_MultipleDelegateParams_AllParamsTreatedAsAttributed_Combinatorial()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public MyService(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) { }
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public MyService(Func<int> fetchCapacity, Func<Task<string>> fetchName)
                : this(() => Task.FromResult(fetchCapacity()), fetchName) { }
            """);
        TestContextHelper.NormalisedContains(source, """
            public MyService(Func<Task<int>> fetchCapacity, Func<string> fetchName)
                : this(fetchCapacity, () => Task.FromResult(fetchName())) { }
            """);
        TestContextHelper.NormalisedContains(source, """
            public MyService(Func<int> fetchCapacity, Func<string> fetchName)
                : this(() => Task.FromResult(fetchCapacity()), () => Task.FromResult(fetchName())) { }
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Class level
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClassLevel_AllMethodsInClass_GetOverloads()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> ProcessA(Func<Task<int>> fetch) => fetch();
                public Task<string> ProcessB(Func<Task<string>> fetch) => fetch();
            }
            """);

        Assert.Contains("#region ProcessA", source);
        Assert.Contains("#region ProcessB", source);
        TestContextHelper.NormalisedContains(source, """
            public Task<int> ProcessA(Func<int> fetch) => ProcessA(() => Task.FromResult(fetch()));
            """);
        TestContextHelper.NormalisedContains(source, """
            public Task<string> ProcessB(Func<string> fetch) => ProcessB(() => Task.FromResult(fetch()));
            """);
    }

    [Fact]
    public void ClassLevel_AllConstructorsInClass_GetOverloads()
    {
        var source = Generate("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public MyService(Func<Task<int>> fetchCapacity) { }
                public MyService(Func<Task<string>> fetchName, int extra) { }
            }
            """);

        TestContextHelper.NormalisedContains(source, """
            public MyService(Func<int> fetchCapacity) : this(() => Task.FromResult(fetchCapacity())) { }
            """);
        TestContextHelper.NormalisedContains(source, """
            public MyService(Func<string> fetchName, int extra) : this(() => Task.FromResult(fetchName()), extra) { }
            """);
    }

    [Fact]
    public void ClassLevel_EquivalentToAttributingEveryMethodAndConstructor()
    {
        var classLevel = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public MyService(Func<Task<int>> fetch) { }
                public Task<int> ProcessA(Func<Task<int>> fetch) => fetch();
            }
            """);

        var perMember = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public MyService(Func<Task<int>> fetch) { }
                [MethodOverloadGeneratorAttribute]
                public Task<int> ProcessA(Func<Task<int>> fetch) => fetch();
            }
            """);

        // Class-level placement fans out into one file with a region per member, rather than
        // one file per attribute usage — the number of generated files therefore differs — but
        // the generated overload bodies are identical either way.
        var classLevelSource = Assert.Single(classLevel.GeneratedSources);
        Assert.Equal(2, perMember.GeneratedSources.Count);

        foreach (var perMemberSource in perMember.GeneratedSources)
        {
            var body = ExtractBetween(perMemberSource, "public partial class MyService\n{", "\n}");
            TestContextHelper.NormalisedContains(classLevelSource, body);
        }
    }

    [Fact]
    public void ClassLevel_SkipsMethodsWithOutParams_Silently()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void WithOutParam(Func<Task<int>> fetch, out int captured) => captured = 0;
                public Task<int> Eligible(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Diagnostics);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Eligible", source);
        Assert.DoesNotContain("WithOutParam", source);
    }

    [Fact]
    public void ClassLevel_SkipsMethodsWithRefParams_Silently()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void WithRefParam(Func<Task<int>> fetch, ref int counter) { }
                public Task<int> Eligible(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Diagnostics);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Eligible", source);
        Assert.DoesNotContain("WithRefParam", source);
    }

    [Fact]
    public void ClassLevel_SkipsNonDelegateOnlyMethods_Silently()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task<int> OnlyNonDelegateParams(int capacity) => Task.FromResult(capacity);
                public Task<int> Eligible(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Diagnostics);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Eligible", source);
        Assert.DoesNotContain("OnlyNonDelegateParams", source);
    }

    [Fact]
    public void ClassLevel_SkipsMethodsThatWouldProduceNoOverloads_Silently()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using MethodOverloadGenerator;
            using System.Threading.Tasks;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public void ActionNoParams(Action action) { }
                public Task<int> Eligible(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Diagnostics);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Eligible", source);
        Assert.DoesNotContain("ActionNoParams", source);
    }

    [Fact]
    public void ClassLevel_OnStaticExtensionClass_AllMethodsGetOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public static partial class AnimalExtensions
            {
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
                public static Task<IPrey> Hunt(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        Assert.Empty(result.Diagnostics);
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public static async Task<IPrey> Hunt<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Hunt();");
    }

    [Fact]
    public void ClassLevel_DoesNotAffectOtherClasses()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class ServiceA
            {
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            public partial class ServiceB
            {
                public Task<int> Process(Func<Task<int>> fetch) => fetch();
            }
            """);

        var source = Assert.Single(result.GeneratedSources);
        Assert.Contains("ServiceA", source);
        Assert.DoesNotContain("ServiceB", source);
    }

    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    private static string Normalise(string source)
        => source.Replace("\r\n", "").Replace("\n", "").Replace(" ", "").Trim();

    private static string ExtractBetween(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal) + start.Length;
        var endIndex = source.LastIndexOf(end, StringComparison.Ordinal);
        return source[startIndex..endIndex];
    }
}
