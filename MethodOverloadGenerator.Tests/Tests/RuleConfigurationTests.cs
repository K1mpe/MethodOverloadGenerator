namespace MethodOverloadGenerator.Tests;

/// <summary>
/// Tests for the per-rule enable/disable configuration system.
///
/// Each rule has a nullable bool parameter on [MethodOverloadGenerator] and a corresponding
/// MSBuild property.  Resolution order (highest priority first):
///   1. Attribute parameter (false / true / null)
///   2. MSBuild property  (false / true / not set)
///   3. Built-in default  (enabled)
///
/// A rule runs unless it has been explicitly set to false somewhere in that chain.
/// null means "defer to the next level down".
/// </summary>
public class RuleConfigurationTests
{
    // -----------------------------------------------------------------------------------------
    // Rule 1 (syncOverloads) — attribute parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule1_AttributeParam_False_SuppressesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.DoesNotContain("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule1_AttributeParam_True_GeneratesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule1_AttributeParam_Null_FallsBackToDefault_GeneratesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Default)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 2 (valueOverloads) — attribute parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule2_AttributeParam_False_SuppressesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.DoesNotContain("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule2_AttributeParam_True_GeneratesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Enable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule2_AttributeParam_Null_FallsBackToDefault_GeneratesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Default)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 3 (trailingParameterOverloads) — attribute parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule3_AttributeParam_False_SuppressesTrailingParameterOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.DoesNotContain("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule3_AttributeParam_True_GeneratesTrailingParameterOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Enable)]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule3_AttributeParam_Null_FallsBackToDefault_GeneratesTrailingParameterOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Default)]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 4 (taskReceiverOverloads) — attribute parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule4_AttributeParam_False_SuppressesTaskReceiverOverloads()
    {
        // No delegate parameters at all — Rule 4 is the only rule that could ever apply, so
        // disabling it leaves nothing to generate.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Disable)]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Rule4_AttributeParam_True_GeneratesTaskReceiverOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Enable)]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        Assert.Contains("Feed<TCarnivore>(this Task<TCarnivore> animal)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule4_AttributeParam_Null_FallsBackToDefault_GeneratesTaskReceiverOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute(taskReceiverOverloads: RuleOverride.Default)]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        Assert.Contains("Feed<TCarnivore>(this Task<TCarnivore> animal)", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Rule 5 (combinatorialOverloads) — attribute parameter
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule5_AttributeParam_False_SuppressesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(combinatorialOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.DoesNotContain("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule5_AttributeParam_True_GeneratesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(combinatorialOverloads: RuleOverride.Enable)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule5_AttributeParam_Null_FallsBackToDefault_GeneratesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(combinatorialOverloads: RuleOverride.Default)]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // MSBuild property — Rule 1
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule1_MsBuildPropertyFalse_SuppressesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false" });

        Assert.DoesNotContain("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule1_MsBuildPropertyTrue_GeneratesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "true" });

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule1_MsBuildPropertyNotSet_BuiltInDefaultIsEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // MSBuild property — Rule 2
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule2_MsBuildPropertyFalse_SuppressesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_ValueOverloads"] = "false" });

        Assert.DoesNotContain("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule2_MsBuildPropertyTrue_GeneratesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_ValueOverloads"] = "true" });

        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule2_MsBuildPropertyNotSet_BuiltInDefaultIsEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // MSBuild property — Rule 3
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule3_MsBuildPropertyFalse_SuppressesTrailingParameterOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_TrailingParameterOverloads"] = "false" });

        Assert.DoesNotContain("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule3_MsBuildPropertyTrue_GeneratesTrailingParameterOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_TrailingParameterOverloads"] = "true" });

        Assert.Contains("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule3_MsBuildPropertyNotSet_BuiltInDefaultIsEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        Assert.Contains("Func<int, Task<bool>> canAdmit", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // MSBuild property — Rule 4
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule4_MsBuildPropertyFalse_SuppressesTaskReceiverOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """,
            new() { ["build_property.MethodOverloadGenerator_TaskReceiverOverloads"] = "false" });

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Rule4_MsBuildPropertyTrue_GeneratesTaskReceiverOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """,
            new() { ["build_property.MethodOverloadGenerator_TaskReceiverOverloads"] = "true" });

        Assert.Contains("Feed<TCarnivore>(this Task<TCarnivore> animal)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule4_MsBuildPropertyNotSet_BuiltInDefaultIsEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        Assert.Contains("Feed<TCarnivore>(this Task<TCarnivore> animal)", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // MSBuild property — Rule 5
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule5_MsBuildPropertyFalse_SuppressesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_CombinatorialOverloads"] = "false" });

        Assert.DoesNotContain("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule5_MsBuildPropertyTrue_GeneratesCombinatorialOverloads()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_CombinatorialOverloads"] = "true" });

        Assert.Contains("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule5_MsBuildPropertyNotSet_BuiltInDefaultIsEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task<int>> p1, Func<Task<string>> p2) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Func<int> p1, Func<string> p2)", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Resolution order — attribute overrides MSBuild property
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AttributeFalse_OverridesMsBuildTrue_RuleDisabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "true" });

        Assert.DoesNotContain("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void AttributeTrue_OverridesMsBuildFalse_RuleEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false" });

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void AttributeNull_DefersToCsproj_WhenCsprojFalse_RuleDisabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false" });

        Assert.DoesNotContain("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void AttributeNull_DefersToCsproj_WhenCsprojTrue_RuleEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "true" });

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void AttributeNull_CsprojNotSet_BuiltInDefaultApplies_RuleEnabled()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Partial rule sets — disabling one rule does not affect others
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Rule1Disabled_Rule2StillGeneratesValueOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("int fetchValue", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule2Disabled_Rule1StillGeneratesSyncOverload()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(valueOverloads: RuleOverride.Disable)]
                public Task<int> Process(Func<Task<int>> fetch) => Task.FromResult(0);
            }
            """);

        Assert.Contains("Func<int> fetch", result.SingleGeneratedSource);
    }

    [Fact]
    public void Rule3Disabled_Rule1AndRule2StillApply()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(trailingParameterOverloads: RuleOverride.Disable)]
                public Task<bool> Process(Func<int, string, Task<bool>> canAdmit) => Task.FromResult(true);
            }
            """);

        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int, string, bool> canAdmit", source);
        Assert.Contains("bool canAdmitValue", source);
    }

    [Fact]
    public void Rule4Disabled_Rule1AndRule2StillApplyToExtensionMethod()
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

        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int> fetchAmount", source);
        Assert.Contains("int fetchAmountValue", source);
    }

    [Fact]
    public void Rule5Disabled_SingleParamOverloadsStillGenerated()
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
        Assert.Contains("Func<int> p1, Func<Task<string>> p2)", source);
        Assert.Contains("Func<Task<int>> p1, Func<string> p2)", source);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute placement — rule config scope
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ParameterLevel_RuleConfig_AffectsOnlyThatParameter()
        // Rule disabled on one parameter does not affect other parameters of the same method
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                public Task<int> Process(
                    [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)] Func<Task<int>> fetchA,
                    [MethodOverloadGeneratorAttribute] Func<Task<string>> fetchB) => Task.FromResult(0);
            }
            """);

        var combined = string.Join("\n", result.GeneratedSources);
        Assert.DoesNotContain("Func<int> fetchA", combined);
        Assert.Contains("Func<string> fetchB", combined);
    }

    [Fact]
    public void MethodLevel_RuleConfig_AffectsAllDelegateParamsInThatMethod()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task Process(Func<Task<int>> fetchA, Func<Task<int>> fetchB) => Task.CompletedTask;
            }
            """);

        var source = result.SingleGeneratedSource;
        Assert.DoesNotContain("Func<int> fetchA", source);
        Assert.DoesNotContain("Func<int> fetchB", source);
        Assert.Contains("int fetchAValue", source);
        Assert.Contains("int fetchBValue", source);
    }

    [Fact]
    public void MethodLevel_RuleConfig_DoesNotAffectOtherMethodsInClass()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task<int> ProcessA(Func<Task<int>> fetchA) => Task.FromResult(0);

                [MethodOverloadGeneratorAttribute]
                public Task<string> ProcessB(Func<Task<string>> fetchB) => Task.FromResult("");
            }
            """);

        var combined = string.Join("\n", result.GeneratedSources);
        Assert.DoesNotContain("Func<int> fetchA", combined);
        Assert.Contains("Func<string> fetchB", combined);
    }

    [Fact]
    public void ClassLevel_RuleConfig_AffectsAllMethodsAndConstructorsInClass()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                public MyService(Func<Task<int>> fetchCtor) { }
                public Task<int> Process(Func<Task<int>> fetchMethod) => Task.FromResult(0);
            }
            """);

        var source = result.SingleGeneratedSource;
        Assert.DoesNotContain("Func<int> fetchCtor", source);
        Assert.DoesNotContain("Func<int> fetchMethod", source);
        Assert.Contains("int fetchCtorValue", source);
        Assert.Contains("int fetchMethodValue", source);
    }

    [Fact]
    public void ClassLevel_RuleConfig_DoesNotAffectOtherClasses()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
            public partial class ServiceA
            {
                public Task<int> Process(Func<Task<int>> fetchA) => Task.FromResult(0);
            }
            [MethodOverloadGeneratorAttribute]
            public partial class ServiceB
            {
                public Task<int> Process(Func<Task<int>> fetchB) => Task.FromResult(0);
            }
            """);

        var combined = string.Join("\n", result.GeneratedSources);
        Assert.DoesNotContain("Func<int> fetchA", combined);
        Assert.Contains("Func<int> fetchB", combined);
    }

    // -----------------------------------------------------------------------------------------
    // Attribute placement — more specific level overrides less specific
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MethodLevel_RuleConfig_OverridesClassLevel_ForThatMethod()
        // Class says false, method says true → rule applies for that method
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)]
                public Task Process(Func<Task> action) => Task.CompletedTask;
            }
            """);

        // The class-level candidate disables Rule 1 for Process, leaving it with zero eligible
        // overloads, so it's silently skipped there — only the method's own attribute (which
        // re-enables Rule 1 for this method) actually produces a generated file.
        Assert.Contains("Action action", result.SingleGeneratedSource);
    }

    [Fact]
    public void MethodLevel_RuleConfig_Null_DoesNotInheritFromClassLevel_FallsBackToCsproj()
        // Class says false, method says null → null defers to csproj/default, not class
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute]
                public Task Process(Func<Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action action", result.SingleGeneratedSource);
    }

    [Fact]
    public void ParameterLevel_RuleConfig_OverridesMethodLevel_ForThatParameter()
        // Method says false, parameter says true → rule applies for that parameter
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task Process([MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)] Func<Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action action", result.SingleGeneratedSource);
    }

    [Fact]
    public void ParameterLevel_RuleConfig_Null_DoesNotInheritFromMethodLevel_FallsBackToCsproj()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public Task Process([MethodOverloadGeneratorAttribute] Func<Task> action) => Task.CompletedTask;
            }
            """);

        Assert.Contains("Action action", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Constructor-level rule config
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ConstructorLevel_RuleConfig_AffectsAllDelegateParamsOfConstructor()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Enable)]
                public MyService(Func<Task<int>> fetchA, Func<Task<int>> fetchB) { }
            }
            """,
            new() { ["build_property.MethodOverloadGenerator_SyncOverloads"] = "false" });

        var source = result.SingleGeneratedSource;
        Assert.Contains("Func<int> fetchA", source);
        Assert.Contains("Func<int> fetchB", source);
    }

    [Fact]
    public void ConstructorLevel_RuleConfig_False_SuppressesRuleForAllParams()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class MyService
            {
                [MethodOverloadGeneratorAttribute(syncOverloads: RuleOverride.Disable)]
                public MyService(Func<Task<int>> fetchA, Func<Task<int>> fetchB) { }
            }
            """);

        var source = result.SingleGeneratedSource;
        Assert.DoesNotContain("Func<int> fetchA", source);
        Assert.DoesNotContain("Func<int> fetchB", source);
        Assert.Contains("int fetchAValue", source);
        Assert.Contains("int fetchBValue", source);
    }
}
