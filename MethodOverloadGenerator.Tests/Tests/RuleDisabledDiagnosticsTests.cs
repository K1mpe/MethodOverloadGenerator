using Microsoft.CodeAnalysis;

namespace MethodOverloadGenerator.Tests;

/// <summary>
/// Diagnostics emitted when [MethodOverloadGenerator] is present but every rule that would
/// apply to that element has been explicitly disabled — either via attribute parameters or
/// MSBuild properties.
///
/// This is distinct from the "no overloads possible" diagnostic in DiagnosticsTests, which
/// covers delegates to which no rule can ever apply regardless of configuration (e.g. Action
/// with no parameters).  Here the delegate IS compatible with at least one rule, but that rule
/// has been deliberately turned off.
///
/// Severity follows the same placement rules as the existing "no overloads" diagnostic:
///   • Method / parameter level  → warning (explicit opt-in, user should notice)
///   • Class level               → silently skipped (bulk opt-in, not every method needs overloads)
/// </summary>
public class RuleDisabledDiagnosticsTests
{
    // -----------------------------------------------------------------------------------------
    // All applicable rules disabled via attribute parameters
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllApplicableRules_DisabledViaAttributeParam_ParameterLevel_ReportsWarning()
    {
        // Func<Task<int>> is eligible for both Rule 1 (sync) and Rule 2 (fixed value) — disabling
        // both leaves nothing to generate.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task Process([MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, valueOverloads: RuleOverride.Disable)] Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void AllApplicableRules_DisabledViaAttributeParam_MethodLevel_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, valueOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void AllApplicableRules_DisabledViaAttributeParam_ClassLevel_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AllApplicableRules_DisabledViaAttributeParam_ClassLevel_OtherMethodsUnaffected()
    {
        // Class-level config is uniform, but "all applicable rules disabled" depends on what's
        // applicable to each method's own delegate — Process only has Rule 2 available (disabled,
        // so it's skipped); Fetch also has Rule 1 available (still enabled, so it still gets overloads).
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public Task Process(Func<int> getValue) => Task.CompletedTask;
                public Task<string> Fetch(Func<Task<string>> fetchName) => Task.FromResult("");
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains("Func<string> fetchName", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // All applicable rules disabled via MSBuild properties
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllApplicableRules_DisabledViaMsBuild_ParameterLevel_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task Process([MethodOverloadGeneratorAttribute] Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """,
            new()
            {
                ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false",
                ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false",
            });

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void AllApplicableRules_DisabledViaMsBuild_MethodLevel_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """,
            new()
            {
                ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false",
                ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false",
            });

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void AllApplicableRules_DisabledViaMsBuild_ClassLevel_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class MyService
            {
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """,
            new()
            {
                ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false",
                ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false",
            });

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.GeneratedSources);
    }

    // -----------------------------------------------------------------------------------------
    // Only one applicable rule, and it is disabled
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncT_Sync_OnlyRule2Applies_Rule2Disabled_ReportsWarning()
    {
        // Func<int> is already synchronous — Rule 1 (sync overload) doesn't apply, so Rule 2
        // (fixed value) is the only option.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
                public Task Process(Func<int> getValue) => Task.CompletedTask;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void FuncTaskVoid_OnlyRule1Applies_Rule1Disabled_ReportsWarning()
    {
        // Func<Task> has no return value, so Rule 2 (fixed value) doesn't apply — Rule 1
        // (Action overload) is the only option.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task> runAsync) => Task.CompletedTask;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void ExtensionMethod_NoDelegateParams_OnlyRule4Applies_Rule4Disabled_ReportsWarning()
    {
        // No delegate parameters at all — Rule 4 (Task<T>/ValueTask<T> receiver) is the only
        // rule an extension method can ever qualify for on its own.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class CatExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static void Sedate(this Cat cat) { }
            }
            public class Cat {}
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    // -----------------------------------------------------------------------------------------
    // Partially disabled — at least one applicable rule remains → no warning
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void FuncTaskT_Rule1Disabled_Rule2StillApplies_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void FuncTaskT_Rule2Disabled_Rule1StillApplies_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains("Func<int> fetch)", result.SingleGeneratedSource);
    }

    [Fact]
    public void MultiParamDelegate_Rule3Disabled_Rules1And2StillApply_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int, string, bool> canAdmit", source);
        Assert.Contains("bool canAdmitValue", source);
    }

    [Fact]
    public void ExtensionMethod_Rule4Disabled_DelegateParamRulesStillApply_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class CatExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static Task Feed(this Cat cat, Func<Task<int>> fetchAmount) => Task.CompletedTask;
            }
            public class Cat {}
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains("Func<int> fetchAmount", result.SingleGeneratedSource);
    }

    [Fact]
    public void MultipleAttributedParams_Rule5Disabled_SingleParamRulesStillApply_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(combinatorialOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int> p1, Func<Task<string>> p2)", source);
        Assert.Contains("Func<Task<int>> p1, Func<string> p2)", source);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute re-enables a rule that MSBuild disabled → no warning
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AllRulesDisabledViaMsBuild_AttributeReEnablesOne_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """,
            new()
            {
                ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false",
                ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false",
            });

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains("Func<int> fetch)", result.SingleGeneratedSource);
    }

    [Fact]
    public void AllRulesDisabledViaMsBuild_AttributeReEnablesAll_NoWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable, valueOverloads: RuleOverride.Enable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """,
            new()
            {
                ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false",
                ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false",
            });

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int> fetch)", source);
        Assert.Contains("int fetchValue", source);
    }

    // -----------------------------------------------------------------------------------------
    // Warning is distinguishable from "delegate type incompatible" warning
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Warning_Message_MentionsDisabledRules_NotIncompatibleDelegateType()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, valueOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        var message = Assert.Single(result.Warnings).GetMessage();
        Assert.Contains("disabled", message);
        Assert.DoesNotContain("no delegate parameters are eligible", message);
    }

    [Fact]
    public void Warning_DiagnosticId_DifferentFrom_NoOverloadsPossibleDiagnostic()
    {
        var rulesDisabled = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable, valueOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetch) => Task.CompletedTask;
            }
            """);

        var noOverloadsPossible = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Action doIt) => Task.CompletedTask;
            }
            """);

        Assert.Equal("MOG002", Assert.Single(rulesDisabled.Warnings).Id);
        Assert.Equal("MOG001", Assert.Single(noOverloadsPossible.Warnings).Id);
    }

    // -----------------------------------------------------------------------------------------
    // Class level — mixed: some methods have all rules disabled, others do not
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ClassLevel_SomeMethodsAllRulesDisabled_ThoseMethodsSilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public Task Process(Func<int> getValue) => Task.CompletedTask;
                public Task<string> Fetch(Func<Task<string>> fetchName) => Task.FromResult("");
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.DoesNotContain("Process(", result.SingleGeneratedSource);
    }

    [Fact]
    public void ClassLevel_SomeMethodsAllRulesDisabled_OtherMethodsStillGetOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public Task Process(Func<int> getValue) => Task.CompletedTask;
                public Task<string> Fetch(Func<Task<string>> fetchName) => Task.FromResult("");
            }
            """);

        Assert.Contains("Func<string> fetchName", result.SingleGeneratedSource);
    }

    [Fact]
    public void ClassLevel_AllMethodsAllRulesDisabled_NoOutputGenerated()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public Task Process(Func<int> getValue) => Task.CompletedTask;
                public Task Compute(Func<int> getOther) => Task.CompletedTask;
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.GeneratedSources);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 5 specific — combinatorial disabled but individual param rules still run
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MultipleAttributedParams_Rule5Disabled_EachParamStillGetsItsOwnOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(combinatorialOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        var source = result.SingleGeneratedSource;

        // Each parameter substituted alone...
        Assert.Contains("Func<int> p1, Func<Task<string>> p2)", source);
        Assert.Contains("Func<Task<int>> p1, Func<string> p2)", source);

        // ...but never both at once (that would be Rule 5's job).
        Assert.DoesNotContain("Func<int> p1, Func<string> p2)", source);
    }

    [Fact]
    public void MultipleAttributedParams_AllRulesDisabled_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(
                    syncOverloads: RuleOverride.Disable,
                    valueOverloads: RuleOverride.Disable,
                    combinatorialOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    // -----------------------------------------------------------------------------------------
    // Constructor-specific
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_AllApplicableRulesDisabled_MethodLevel_ReportsWarning()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
                public MyService(Func<int> getValue) { }
            }
            """);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("MOG002", warning.Id);
    }

    [Fact]
    public void Constructor_AllApplicableRulesDisabled_ClassLevel_SilentlySkipped()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public MyService(Func<int> getValue) { }
            }
            """);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.GeneratedSources);
    }
}
