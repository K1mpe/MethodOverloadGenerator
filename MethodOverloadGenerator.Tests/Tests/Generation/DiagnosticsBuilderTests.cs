using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Generation;

namespace MethodOverloadGenerator.Tests.Generation;

public class DiagnosticsBuilderTests
{
    private readonly DiagnosticsBuilder _sut = new();

    // -----------------------------------------------------------------------------------------
    // Helpers — build MasterContext without going through Roslyn compilation.
    // DiagnosticsBuilder only reads: IsPartialClass, ClassName, MethodName,
    // AttributePlacement, HasOutOrRefParams, NonDelegateAttributedParamName,
    // AnyApplicableRuleDisabled, and ApplyRule1..5.  All other fields are irrelevant.
    // -----------------------------------------------------------------------------------------

    private static DeclarationContext Declaration(
        bool   isPartial  = true,
        string className  = "TestClass",
        string methodName = "TestMethod") => new()
    {
        Namespace           = null,
        ClassName           = className,
        MethodName          = methodName,
        IsConstructor       = false,
        AccessModifier      = AccessModifier.Public,
        IsStatic            = false,
        ClassAccessModifier = AccessModifier.Public,
        IsStaticClass       = false,
        IsExtensionMethod   = false,
        IsPartialClass      = isPartial,
        ReturnType          = "void",
        Parameters          = [],
        Documentation       = DocumentationComment.None,
        Usings              = [],
        TypeParameters      = [],
        TypeParameterConstraintClauses = [],
    };

    private static AllowedRulesContext AllAllowed => new()
    {
        AllowRule1 = true, AllowRule2 = true, AllowRule3 = true,
        AllowRule4 = true, AllowRule5 = true,
    };

    // A context that would generate overloads (ApplyRule1 = true) — represents the happy path.
    private static MasterContext WithOverloads(
        AttributePlacement placement  = AttributePlacement.Method,
        bool               isPartial  = true,
        string             className  = "TestClass",
        string             methodName = "TestMethod") => new()
    {
        Declaration                    = Declaration(isPartial, className, methodName),
        AllowedRules                   = AllAllowed,
        AttributePlacement             = placement,
        AttributeLocation              = Location.None,
        HasOutOrRefParams              = false,
        NonDelegateAttributedParamName = null,
        AnyApplicableRuleDisabled      = false,
        ApplyRule1 = true,
        ApplyRule2 = false,
        ApplyRule3 = false,
        ApplyRule4 = false,
        ApplyRule5 = false,
        Rule1Contexts = [new Rule1Context { Delegate = AnyDelegate }],
        Rule2Contexts = null,
        Rule3Contexts = null,
        Rule4Context  = null,
        Rule5Context  = null,
    };

    // A context where no overloads will be generated.
    private static MasterContext WithNoOverloads(
        AttributePlacement placement        = AttributePlacement.Method,
        bool               anyRuleDisabled  = false,
        bool               hasOutRef        = false,
        string?            nonDelegateParam = null,
        bool               isPartial        = true) => new()
    {
        Declaration                    = Declaration(isPartial),
        AllowedRules                   = AllAllowed,
        AttributePlacement             = placement,
        AttributeLocation              = Location.None,
        HasOutOrRefParams              = hasOutRef,
        NonDelegateAttributedParamName = nonDelegateParam,
        AnyApplicableRuleDisabled      = anyRuleDisabled,
        ApplyRule1 = false,
        ApplyRule2 = false,
        ApplyRule3 = false,
        ApplyRule4 = false,
        ApplyRule5 = false,
        Rule1Contexts = null,
        Rule2Contexts = null,
        Rule3Contexts = null,
        Rule4Context  = null,
        Rule5Context  = null,
    };

    private static DelegateInfo AnyDelegate => new()
    {
        ParameterName = "f",
        InputTypes    = [],
        ReturnType    = null,
        IsAsync       = true,
        IsValueTask   = false,
    };

    // -----------------------------------------------------------------------------------------
    // Non-partial class (MOG003) — always an error regardless of placement
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData((int)AttributePlacement.Class)]
    [InlineData((int)AttributePlacement.Method)]
    [InlineData((int)AttributePlacement.Parameter)]
    public void NonPartialClass_ReportsError_AtAllPlacementLevels(int rawPlacement)
    {
        var context = WithOverloads((AttributePlacement)rawPlacement, isPartial: false);

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal(DiagnosticSeverity.Error, result[0].Severity);
    }

    [Fact]
    public void NonPartialClass_Error_HasId_MOG003()
    {
        var result = _sut.Build(WithOverloads(isPartial: false));

        Assert.Equal("MOG003", result[0].Descriptor.Id);
    }

    [Fact]
    public void NonPartialClass_Error_MentionsClassName()
    {
        var context = WithOverloads(isPartial: false, className: "MyService");

        var result = _sut.Build(context);

        Assert.Contains("MyService", result[0].GetMessage());
    }

    [Fact]
    public void NonPartialClass_TakesPriorityOver_OutOrRefParams()
    {
        var context = WithNoOverloads(isPartial: false, hasOutRef: true);

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal("MOG003", result[0].Descriptor.Id);
    }

    // -----------------------------------------------------------------------------------------
    // out/ref parameters (MOG004) — error at method/parameter level, silent at class level
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData((int)AttributePlacement.Method)]
    [InlineData((int)AttributePlacement.Parameter)]
    public void OutOrRefParams_ReportsError_AtMethodAndParameterLevel(int rawPlacement)
    {
        var context = WithNoOverloads((AttributePlacement)rawPlacement, hasOutRef: true);

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal(DiagnosticSeverity.Error, result[0].Severity);
    }

    [Fact]
    public void OutOrRefParams_ReturnsEmpty_AtClassLevel()
    {
        var context = WithNoOverloads(AttributePlacement.Class, hasOutRef: true);

        Assert.Empty(_sut.Build(context));
    }

    [Fact]
    public void OutOrRefParams_Error_HasId_MOG004()
    {
        var result = _sut.Build(WithNoOverloads(hasOutRef: true));

        Assert.Equal("MOG004", result[0].Descriptor.Id);
    }

    [Fact]
    public void OutOrRefParams_Error_MentionsMethodName()
    {
        var context = WithNoOverloads(hasOutRef: true) with
        {
            Declaration = Declaration(methodName: "ProcessData"),
        };

        Assert.Contains("ProcessData", _sut.Build(context)[0].GetMessage());
    }

    // -----------------------------------------------------------------------------------------
    // Non-delegate attributed parameter (MOG005) — always an error (only set at param level)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void NonDelegateParam_ReportsError_WhenParamNameIsSet()
    {
        var context = WithNoOverloads(
            AttributePlacement.Parameter,
            nonDelegateParam: "capacity");

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal(DiagnosticSeverity.Error, result[0].Severity);
    }

    [Fact]
    public void NonDelegateParam_DoesNotReportMog005_WhenParamNameIsNull()
    {
        // Overloads are generated, param name is null → no diagnostic at all.
        Assert.DoesNotContain(_sut.Build(WithOverloads()), d => d.Descriptor.Id == "MOG005");
    }

    [Fact]
    public void NonDelegateParam_Error_HasId_MOG005()
    {
        var result = _sut.Build(WithNoOverloads(nonDelegateParam: "capacity"));

        Assert.Equal("MOG005", result[0].Descriptor.Id);
    }

    [Fact]
    public void NonDelegateParam_Error_MentionsParameterName()
    {
        var result = _sut.Build(WithNoOverloads(nonDelegateParam: "myCapacity"));

        Assert.Contains("myCapacity", result[0].GetMessage());
    }

    [Fact]
    public void NonDelegateParam_TakesPriorityOver_NoOverloads()
    {
        // Ensures the non-delegate check runs before the "no overloads" check.
        var context = WithNoOverloads(
            AttributePlacement.Parameter,
            anyRuleDisabled: true,
            nonDelegateParam: "capacity");

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal("MOG005", result[0].Descriptor.Id);
    }

    // -----------------------------------------------------------------------------------------
    // No overloads possible (MOG001) — warning at method/parameter level, silent at class level
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData((int)AttributePlacement.Method)]
    [InlineData((int)AttributePlacement.Parameter)]
    public void NoOverloadsPossible_ReportsWarning_AtMethodAndParameterLevel(int rawPlacement)
    {
        var context = WithNoOverloads((AttributePlacement)rawPlacement, anyRuleDisabled: false);

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal(DiagnosticSeverity.Warning, result[0].Severity);
    }

    [Fact]
    public void NoOverloadsPossible_ReturnsEmpty_AtClassLevel()
    {
        var context = WithNoOverloads(AttributePlacement.Class, anyRuleDisabled: false);

        Assert.Empty(_sut.Build(context));
    }

    [Fact]
    public void NoOverloadsPossible_Warning_HasId_MOG001()
    {
        var result = _sut.Build(WithNoOverloads(anyRuleDisabled: false));

        Assert.Equal("MOG001", result[0].Descriptor.Id);
    }

    [Fact]
    public void NoOverloadsPossible_Warning_MentionsMethodName()
    {
        var context = WithNoOverloads() with
        {
            Declaration = Declaration(methodName: "FetchDogs"),
        };

        Assert.Contains("FetchDogs", _sut.Build(context)[0].GetMessage());
    }

    // -----------------------------------------------------------------------------------------
    // All applicable rules disabled (MOG002) — warning at method/parameter, silent at class
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData((int)AttributePlacement.Method)]
    [InlineData((int)AttributePlacement.Parameter)]
    public void AllRulesDisabled_ReportsWarning_AtMethodAndParameterLevel(int rawPlacement)
    {
        var context = WithNoOverloads((AttributePlacement)rawPlacement, anyRuleDisabled: true);

        var result = _sut.Build(context);

        Assert.Single(result);
        Assert.Equal(DiagnosticSeverity.Warning, result[0].Severity);
    }

    [Fact]
    public void AllRulesDisabled_ReturnsEmpty_AtClassLevel()
    {
        var context = WithNoOverloads(AttributePlacement.Class, anyRuleDisabled: true);

        Assert.Empty(_sut.Build(context));
    }

    [Fact]
    public void AllRulesDisabled_Warning_HasId_MOG002()
    {
        var result = _sut.Build(WithNoOverloads(anyRuleDisabled: true));

        Assert.Equal("MOG002", result[0].Descriptor.Id);
    }

    [Fact]
    public void AllRulesDisabled_Warning_HasDifferentId_ThanNoOverloadsPossible()
    {
        var noOverloadsId  = _sut.Build(WithNoOverloads(anyRuleDisabled: false))[0].Descriptor.Id;
        var rulesDisabledId = _sut.Build(WithNoOverloads(anyRuleDisabled: true))[0].Descriptor.Id;

        Assert.NotEqual(noOverloadsId, rulesDisabledId);
    }

    // -----------------------------------------------------------------------------------------
    // IsEligibleForGeneration — used by the generator at class level to skip methods silently
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void IsEligibleForGeneration_ReturnsFalse_WhenNoRulesApply()
    {
        Assert.False(_sut.IsEligibleForGeneration(WithNoOverloads()));
    }

    [Fact]
    public void IsEligibleForGeneration_ReturnsFalse_WhenMethodHasOutOrRefParams()
    {
        // out/ref blocks generation even when rules would otherwise apply.
        var context = WithOverloads() with { HasOutOrRefParams = true };

        Assert.False(_sut.IsEligibleForGeneration(context));
    }

    [Theory]
    [InlineData(true,  false, false, false, false)]
    [InlineData(false, true,  false, false, false)]
    [InlineData(false, false, true,  false, false)]
    [InlineData(false, false, false, true,  false)]
    [InlineData(false, false, false, false, true)]
    [InlineData(true,  true,  true,  true,  true)]
    public void IsEligibleForGeneration_ReturnsTrue_WhenAtLeastOneRuleApplies(
        bool r1, bool r2, bool r3, bool r4, bool r5)
    {
        var context = WithNoOverloads() with
        {
            ApplyRule1 = r1,
            ApplyRule2 = r2,
            ApplyRule3 = r3,
            ApplyRule4 = r4,
            ApplyRule5 = r5,
        };

        Assert.True(_sut.IsEligibleForGeneration(context));
    }

    // -----------------------------------------------------------------------------------------
    // Happy path — returns no diagnostics when at least one rule applies
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData((int)AttributePlacement.Class)]
    [InlineData((int)AttributePlacement.Method)]
    [InlineData((int)AttributePlacement.Parameter)]
    public void ReturnsEmpty_WhenOverloadsWillBeGenerated_AtAllPlacementLevels(int rawPlacement)
    {
        Assert.Empty(_sut.Build(WithOverloads((AttributePlacement)rawPlacement)));
    }

    [Theory]
    [InlineData(true,  false, false, false, false)]
    [InlineData(false, true,  false, false, false)]
    [InlineData(false, false, true,  false, false)]
    [InlineData(false, false, false, true,  false)]
    [InlineData(false, false, false, false, true)]
    [InlineData(true,  true,  true,  true,  true)]
    public void ReturnsEmpty_ForEachCombinationOfAppliedRules(
        bool r1, bool r2, bool r3, bool r4, bool r5)
    {
        var context = WithOverloads() with
        {
            ApplyRule1 = r1,
            ApplyRule2 = r2,
            ApplyRule3 = r3,
            ApplyRule4 = r4,
            ApplyRule5 = r5,
        };

        Assert.Empty(_sut.Build(context));
    }
}
