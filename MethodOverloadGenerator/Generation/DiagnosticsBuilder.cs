using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Inspects a <see cref="MasterContext"/> and produces any diagnostics (errors / warnings)
/// that should be reported before code generation runs.
/// Examples: attribute present but no rule is eligible; attribute used on a parameter
/// that is not a delegate type.
/// </summary>
internal sealed class DiagnosticsBuilder
{
    private const string Category = "MethodOverloadGenerator";

    // Warnings — emitted when the attribute is present at method/parameter level but
    // no overloads would be generated.  Class-level placement is silently skipped instead.

    internal static readonly DiagnosticDescriptor NoOverloadsPossible = new(
        id:                 "MOG001",
        title:              "No overloads can be generated",
        messageFormat:      GeneratorConstants.AttributeDisplayName + " on '{0}' will generate no overloads: " +
                            "no delegate parameters are eligible for any rule",
        category:           Category,
        defaultSeverity:    DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor AllApplicableRulesDisabled = new(
        id:                 "MOG002",
        title:              "All applicable rules are disabled",
        messageFormat:      GeneratorConstants.AttributeDisplayName + " on '{0}' will generate no overloads: " +
                            "all rules that apply to its delegate parameters have been disabled",
        category:           Category,
        defaultSeverity:    DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Errors — always block generation regardless of attribute placement level.

    internal static readonly DiagnosticDescriptor NonPartialClass = new(
        id:                 "MOG003",
        title:              "Containing class must be partial",
        messageFormat:      "Class '{0}' must be declared partial to allow overload generation",
        category:           Category,
        defaultSeverity:    DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor OutOrRefParameters = new(
        id:                 "MOG004",
        title:              "Method has out or ref parameters",
        messageFormat:      "Overloads cannot be generated for '{0}' because it has out or ref parameters",
        category:           Category,
        defaultSeverity:    DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NonDelegateAttributedParameter = new(
        id:                 "MOG005",
        title:              "Attribute placed on a non-delegate parameter",
        messageFormat:      GeneratorConstants.AttributeDisplayName + " was placed on parameter '{0}' which is not a delegate type",
        category:           Category,
        defaultSeverity:    DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Returns <see langword="true"/> when the method represented by <paramref name="context"/>
    /// is eligible for overload generation.
    /// </summary>
    /// <remarks>
    /// The generator calls this at class level to decide whether to process a method or silently
    /// skip it.  Ineligible methods have no delegate-eligible parameters or have out/ref params
    /// that block generation.  This check is separate from <see cref="Build"/> — the class-level
    /// attribute reports no diagnostics either way; it is the generation step that must be gated.
    /// </remarks>
    public bool IsEligibleForGeneration(MasterContext context)
    {
        if (context.HasOutOrRefParams)
            return false;

        return context.ApplyRule1 || context.ApplyRule2 || context.ApplyRule3
            || context.ApplyRule4 || context.ApplyRule5;
    }

    public IReadOnlyList<Diagnostic> Build(MasterContext context)
    {
        // Non-partial class is always an error regardless of attribute placement.
        if (!context.Declaration.IsPartialClass)
            return [Diagnostic.Create(NonPartialClass, context.AttributeLocation, context.Declaration.ClassName)];

        // Class level: no diagnostics — the generator uses IsEligibleForGeneration to decide
        // whether to process a method; ineligible methods are silently skipped.
        if (context.AttributePlacement == AttributePlacement.Class)
            return [];

        // Method/parameter level: report errors and warnings for invalid or unproductive usage.

        if (context.HasOutOrRefParams)
            return [Diagnostic.Create(OutOrRefParameters, context.AttributeLocation, context.Declaration.MethodName)];

        // Only set when AttributePlacement == Parameter, so no class-level guard needed.
        if (context.NonDelegateAttributedParamName is { } paramName)
            return [Diagnostic.Create(NonDelegateAttributedParameter, context.AttributeLocation, paramName)];

        bool anyOverloads = context.ApplyRule1 || context.ApplyRule2 || context.ApplyRule3
                         || context.ApplyRule4 || context.ApplyRule5;
        if (!anyOverloads)
        {
            var descriptor = context.AnyApplicableRuleDisabled
                ? AllApplicableRulesDisabled
                : NoOverloadsPossible;
            return [Diagnostic.Create(descriptor, context.AttributeLocation, context.Declaration.MethodName)];
        }

        return [];
    }
}
