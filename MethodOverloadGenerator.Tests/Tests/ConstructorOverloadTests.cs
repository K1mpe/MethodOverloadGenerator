using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Tests specific to constructors.  The spec treats constructors identically to methods for
/// attribute placement and overload generation; these tests confirm that parity.
/// </summary>
/// <remarks>
/// Constructors can't forward with a plain method call the way ordinary methods do — a
/// constructor named the same as its class calling "ClassName(args)" isn't self-referential
/// chaining, it just doesn't compile. Every rule emitter must instead emit a
/// <c>: this(args) { }</c> constructor initializer with no return type for constructors.
/// </remarks>
public class ConstructorOverloadTests
{
    // -----------------------------------------------------------------------------------------
    // Attribute placement on constructors
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_ParameterLevelAttribute_GeneratesOverloadsForThatParam()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                public AnimalShelter(
                    [MethodOverloadGeneratorAttribute] Func<Task<int>> fetchCapacity,
                    Func<Task<string>> fetchName) { }
            }
            """);

        // Parameter-level placement scopes generation to just the decorated parameter — the
        // unattributed "fetchName" delegate parameter is left out of Rule1Contexts entirely.
        Assert.True(ctx.ApplyRule1);
        Assert.Single(ctx.Rule1Contexts!);
        Assert.Equal("fetchCapacity", ctx.Rule1Contexts![0].Delegate.ParameterName);
    }

    [Fact]
    public void Constructor_ConstructorLevelAttribute_AllDelegateParamsGetOverloads()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) { }
            }
            """);

        Assert.Equal(2, ctx.Rule1Contexts!.Count);
    }

    [Fact]
    public void Constructor_ClassLevelAttribute_GetsOverloads()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class AnimalShelter
            {
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        Assert.True(ctx.ApplyRule1);
        Assert.True(ctx.Declaration.IsConstructor);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 1 — async delegate → sync overload
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_AsyncDelegateParam_GetsSyncOverload()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        var result = new Rule1Emitter().Emit(ctx.Rule1Contexts!, ctx.Declaration);

        Assert.Single(result);
        Assert.Contains("AnimalShelter(Func<int> fetchCapacity)", result[0]);
        Assert.DoesNotContain("void", result[0]);
    }

    [Fact]
    public void Constructor_AsyncDelegateParam_SyncOverload_WrapsWithTaskFromResult()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        var result = new Rule1Emitter().Emit(ctx.Rule1Contexts!, ctx.Declaration);

        // Constructors forward via a ": this(...)" initializer, never "=> MethodName(...)".
        TestContextHelper.NormalisedContains(result[0], """
            public AnimalShelter(Func<int> fetchCapacity)
                : this(() => Task.FromResult(fetchCapacity())) { }
            """);
    }

    [Fact]
    public void Constructor_VoidAsyncDelegateParam_GetsActionOverload()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task> initialize) { }
            }
            """);

        var result = new Rule1Emitter().Emit(ctx.Rule1Contexts!, ctx.Declaration);

        TestContextHelper.NormalisedContains(result[0], """
            public AnimalShelter(Action initialize)
                : this(() => { initialize(); return Task.CompletedTask; }) { }
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 2 — value-returning delegate → fixed-value overload
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_AsyncDelegateParam_GetsFixedValueOverload()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        var result = new Rule2Emitter().Emit(ctx.Rule2Contexts!, ctx.Declaration);

        Assert.Single(result);
        TestContextHelper.NormalisedContains(result[0], """
            public AnimalShelter(int fetchCapacityValue)
                : this(() => Task.FromResult(fetchCapacityValue)) { }
            """);
    }

    [Fact]
    public void Constructor_SyncDelegateParam_GetsFixedValueOverload()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<int> getCapacity) { }
            }
            """);

        var result = new Rule2Emitter().Emit(ctx.Rule2Contexts!, ctx.Declaration);

        Assert.Single(result);
        TestContextHelper.NormalisedContains(result[0], """
            public AnimalShelter(int getCapacityValue)
                : this(() => getCapacityValue) { }
            """);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 3 — trailing parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_MultiInputParamDelegate_GetsTrailingParamOverloads()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<int, string, Task<bool>> canAdmit) { }
            }
            """);

        // N=2 → k=1 and k=0, each with 2 forms (async-drop, fully-sync) = 4 contexts
        Assert.Equal(4, ctx.Rule3Contexts!.Count);
    }

    [Fact]
    public void Constructor_MultiInputParamDelegate_AllSyncVariantsGenerated()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<int, string, Task<bool>> canAdmit) { }
            }
            """);

        var result = new Rule3Emitter().Emit(ctx.Rule3Contexts!, ctx.Declaration);

        Assert.All(result, s => Assert.Contains("AnimalShelter(", s));
        Assert.All(result, s => Assert.Contains(": this(", s));
        Assert.All(result, s => Assert.DoesNotContain("=> AnimalShelter(", s));
    }

    // -----------------------------------------------------------------------------------------
    // Rule 5 — combinatorial overloads
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_MultipleAttributedDelegateParams_CombinatorialOverloadsGenerated()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) { }
            }
            """);

        Assert.NotNull(ctx.Rule5Context);
        var result = new Rule5Emitter().Emit(ctx.Rule5Context!, ctx.Declaration, ctx.AllowedRules, ctx.Rule4Context);

        TestContextHelper.NormalisedContains(result, """
            public AnimalShelter(Func<int> fetchCapacity, Func<string> fetchName)
                : this(() => Task.FromResult(fetchCapacity()), () => Task.FromResult(fetchName())) { }
            """);
    }

    [Fact]
    public void Constructor_ConstructorLevel_MultipleDelegateParams_ProducesCombinatorialOverloads()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class AnimalShelter
            {
                public AnimalShelter(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) { }
            }
            """);

        var result = new Rule5Emitter().Emit(ctx.Rule5Context!, ctx.Declaration, ctx.AllowedRules, ctx.Rule4Context);

        Assert.All(result, s => Assert.Contains(": this(", s));
    }

    // -----------------------------------------------------------------------------------------
    // Diagnostics specific to constructors — DiagnosticsBuilder doesn't branch on IsConstructor
    // at all, so these confirm the same MOG001/MOG003/MOG004 mechanism fires identically for
    // constructors as it does for ordinary methods, via the real end-to-end generator pipeline.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_OutParam_MethodLevelAttribute_ReportsError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity, out int capacity)
                {
                    capacity = 0;
                }
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG004");
    }

    [Fact]
    public void Constructor_RefParam_MethodLevelAttribute_ReportsError()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity, ref int capacity) { }
            }
            """);

        Assert.Contains(result.Errors, d => d.Id == "MOG004");
    }

    [Fact]
    public void Constructor_OutParam_ClassLevelAttribute_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class AnimalShelter
            {
                public AnimalShelter(Func<Task<int>> fetchCapacity, out int capacity)
                {
                    capacity = 0;
                }

                public Task<string> FetchName(Func<Task<string>> fetchName) => fetchName();
            }
            """);

        // The out-param constructor is silently skipped — no error/warning for it — but other
        // eligible members of the same class-level attribute still get their overloads.
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains(result.GeneratedSources, s => s.Contains("FetchName"));
    }

    [Fact]
    public void Constructor_NonPartialClass_AnyAttributeLevel_ReportsError()
    {
        var constructorLevel = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        var classLevel = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public class AnimalShelter
            {
                public AnimalShelter(Func<Task<int>> fetchCapacity) { }
            }
            """);

        Assert.Contains(constructorLevel.Errors, d => d.Id == "MOG003");
        Assert.Contains(classLevel.Errors, d => d.Id == "MOG003");
    }

    [Fact]
    public void Constructor_NoDelegateParams_MethodLevelAttribute_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(int capacity, string name) { }
            }
            """);

        Assert.Contains(result.Warnings, d => d.Id == "MOG001");
    }

    // -----------------------------------------------------------------------------------------
    // Mixed — shelter example from the spec: a full compilation exercising every call-site shape
    // (fully async, mixed sync/async, fully fixed) actually compiles against the real generator.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_MixedFixedAndAsyncArgs_AllCallSiteCombinationsCompile()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            namespace Animal.Services;

            public partial class AnimalShelter
            {
                public int Capacity { get; }
                public TimeSpan NextFeedingTime { get; }

                [MethodOverloadGeneratorAttribute]
                public AnimalShelter(Func<Task<int>> getAvailableCapacity, Func<Task<TimeSpan>> fetchNextFeedingTime)
                {
                    Capacity = getAvailableCapacity().Result;
                    NextFeedingTime = fetchNextFeedingTime().Result;
                }
            }

            public class Usage
            {
                public void Run()
                {
                    // Fully async — delegates from real services
                    var a = new AnimalShelter(() => Task.FromResult(20), () => Task.FromResult(TimeSpan.FromHours(4)));

                    // Mixed — fixed capacity, async feeding schedule
                    var b = new AnimalShelter(20, () => Task.FromResult(TimeSpan.FromHours(4)));

                    // Fully fixed — ideal for tests
                    var c = new AnimalShelter(20, TimeSpan.FromHours(4));

                    // Sync factories
                    var d = new AnimalShelter(() => 20, () => TimeSpan.FromHours(4));
                }
            }
            """;

        var paths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var references = paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();

        var compilation = CSharpCompilation.Create(
            "ConstructorOverloadTestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new global::MethodOverloadGenerator.MethodOverloadGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }
}
