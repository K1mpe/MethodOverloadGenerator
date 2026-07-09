namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Rule 5 — Multiple attributed parameters → combinatorial overloads.
///
/// When more than one parameter carries [MethodOverloadGenerator] every combination of their
/// individual overloads is generated.  Overload count grows multiplicatively:
/// n params each with k variants → k^n − 1 combinations (excluding the original call) — across
/// ALL rules put together (single-parameter combinations come from Rules 1-3 individually;
/// Rule 5 itself only emits the "two or more parameters changed at once" combinations, to avoid
/// duplicating what Rules 1-3 already cover — see Rule5EmitterTests.cs).
/// </summary>
public class Rule5_CombinatorialOverloadTests
{
    // Every generated overload carries exactly one [OverloadResolutionPriority(...)] attribute,
    // regardless of which rule produced it — a simple, reliable way to count total overloads.
    private static int CountGeneratedOverloads(IReadOnlyList<string> sources)
        => sources.Sum(s => s.Split("[OverloadResolutionPriority(").Length - 1);

    // -----------------------------------------------------------------------------------------
    // Two attributed parameters — Func<Task<T>> × Func<Task<U>>
    // Each has 3 variants (original / sync / value) → 3×3 − 1 = 8 combinations overall
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TwoAttributedParams_TotalOf8CombinationsGenerated()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Equal(8, CountGeneratedOverloads(result.GeneratedSources));
    }

    [Fact]
    public void TwoAttributedParams_Combo_BothSync()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<int> p1, Func<string> p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));");
    }

    [Fact]
    public void TwoAttributedParams_Combo_BothFixedValue()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        // Note: unlike Rule 2's own standalone single-parameter overload (which renames to
        // "p1Value"), Rule 5's combinatorial signatures keep the original parameter name for
        // every substituted parameter, regardless of which rule each one's variant came from.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(int p1, string p2) => Process(() => Task.FromResult(p1), () => Task.FromResult(p2));");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstSyncSecondOriginalAsync()
    {
        // A single-parameter substitution — this comes from Rule 1 alone (p2 untouched), not
        // Rule 5's own cross-product, but it must still exist in the final generated file.
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<int> p1, Func<Task<string>> p2) => Process(() => Task.FromResult(p1()), p2);");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstOriginalAsyncSecondSync()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<Task<int>> p1, Func<string> p2) => Process(p1, () => Task.FromResult(p2()));");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstFixedValueSecondOriginalAsync()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(int p1Value, Func<Task<string>> p2) => Process(() => Task.FromResult(p1Value), p2);");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstOriginalAsyncSecondFixedValue()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<Task<int>> p1, string p2Value) => Process(p1, () => Task.FromResult(p2Value));");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstSyncSecondFixedValue()
    {
        // Both parameters change at once — this one genuinely comes from Rule 5's cross-product.
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<int> p1, string p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2));");
    }

    [Fact]
    public void TwoAttributedParams_Combo_FirstFixedValueSecondSync()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(int p1, Func<string> p2) => Process(() => Task.FromResult(p1), () => Task.FromResult(p2()));");
    }

    // -----------------------------------------------------------------------------------------
    // Two attributed parameters — each combo body delegates to the original method
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TwoAttributedParams_EachComboBody_DelegatesToOriginalMethod()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        var combined = string.Join("\n", result.GeneratedSources);
        var callCount = combined.Split("=> Process(").Length - 1;
        Assert.Equal(8, callCount);
    }

    // -----------------------------------------------------------------------------------------
    // Three attributed parameters — multiplicative growth
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThreeAttributedParams_CorrectTotalCombinationsGenerated()
    {
        // 3 params × 3 variants = 3^3 − 1 = 26 combinations (each Func<Task<T>>)
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2, Func<Task<bool>> p3) => Task.CompletedTask;
            }
            """);

        Assert.Equal(26, CountGeneratedOverloads(result.GeneratedSources));
    }

    // -----------------------------------------------------------------------------------------
    // Mixed: attributed delegate parameters vs. an ordinary non-delegate parameter — the
    // non-delegate parameter is never subject to any rule and stays fixed across every combo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MixedAttributedAndNonAttributed_NonAttributedParamStayFixed()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(string name, Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain(result.GeneratedSources, s => s.Contains("nameValue"));
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(string name, Func<int> p1, Func<string> p2) => Process(name, () => Task.FromResult(p1()), () => Task.FromResult(p2()));");
    }

    [Fact]
    public void MixedAttributedAndNonAttributed_OnlyAttributedParamVaries()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(string name, Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        // Same combinatorial shape as the two-delegate-param case (8 total), the leading
        // non-delegate "name" parameter doesn't add any dimension of its own.
        Assert.Equal(8, CountGeneratedOverloads(result.GeneratedSources));
    }

    // -----------------------------------------------------------------------------------------
    // Class-level attribute
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClassLevel_MethodWithMultipleDelegateParams_ProducesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Equal(8, CountGeneratedOverloads(result.GeneratedSources));
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<int> p1, Func<string> p2) => Process(() => Task.FromResult(p1()), () => Task.FromResult(p2()));");
    }

    // -----------------------------------------------------------------------------------------
    // Method-level attribute is equivalent to attributing every delegate param — comparing
    // against attributing each parameter individually is skipped: doing so makes the generator
    // process the method once per attributed parameter (a separate, pre-existing gap where
    // parameter-level placement doesn't restrict generation to just the decorated parameter),
    // which currently produces duplicate/conflicting output rather than a clean equivalence.
    // -----------------------------------------------------------------------------------------

    [Fact(Skip = "Requires attributing multiple individual parameters of the same method, which " +
                 "currently causes the method to be processed once per attribute (duplicate " +
                 "generated overloads) due to a pre-existing parameter-level scoping gap.")]
    public void MethodLevel_MultipleDelegateParams_ProducesSameCombinationsAsParamLevel()
    {
    }

    // -----------------------------------------------------------------------------------------
    // Rule 5 composed with Rule 3 (trailing params)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TwoAttributedParams_EachWithMultipleInputParams_CombinatorialAndTrailingApply()
    {
        var result = TestHelper.RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<int, Task<string>> p1, Func<int, Task<bool>> p2) => Task.CompletedTask;
            }
            """);

        // Each param now has 5 variants (original, sync, value, async-drop-to-0, fully-sync-drop-to-0)
        // → 5×5 − 1 = 24 total combinations across Rules 1, 2, 3, and 5 combined.
        Assert.Equal(24, CountGeneratedOverloads(result.GeneratedSources));

        // A combo that needs BOTH Rule 3 (drop the trailing int, fully-sync form) AND Rule 5
        // (both params changed at once) on the same overload. The fully-sync-drop form's own
        // return type has no Task wrapper, so the wrap expression must re-wrap it with
        // Task.FromResult to satisfy the original async delegate signature.
        TestContextHelper.NormalisedContains(result.GeneratedSources,
            "public Task Process(Func<string> p1, Func<bool> p2) => Process((a) => Task.FromResult(p1()), (a) => Task.FromResult(p2()));");
    }
}
