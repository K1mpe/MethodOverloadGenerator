using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MethodOverloadGenerator.Building.Rules;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Building;

/// <summary>
/// Orchestrates the construction of a <see cref="MasterContext"/> by delegating every
/// part of the work to a dedicated sub-builder.  Contains no domain logic itself.
/// </summary>
internal sealed class MasterContextBuilder
{
    private readonly DeclarationContextBuilder _declarationBuilder;
    private readonly AllowedRulesContextBuilder _allowedRulesBuilder;
    private readonly DelegateParametersBuilder _delegateParamsBuilder;
    private readonly Rule1ContextsBuilder _rule1Builder;
    private readonly Rule2ContextsBuilder _rule2Builder;
    private readonly Rule3ContextsBuilder _rule3Builder;
    private readonly Rule4ContextBuilder _rule4Builder;
    private readonly Rule5ContextBuilder _rule5Builder;

    public MasterContextBuilder(
        DeclarationContextBuilder declarationBuilder,
        AllowedRulesContextBuilder allowedRulesBuilder,
        DelegateParametersBuilder delegateParamsBuilder,
        Rule1ContextsBuilder rule1Builder,
        Rule2ContextsBuilder rule2Builder,
        Rule3ContextsBuilder rule3Builder,
        Rule4ContextBuilder rule4Builder,
        Rule5ContextBuilder rule5Builder)
    {
        _declarationBuilder = declarationBuilder;
        _allowedRulesBuilder = allowedRulesBuilder;
        _delegateParamsBuilder = delegateParamsBuilder;
        _rule1Builder = rule1Builder;
        _rule2Builder = rule2Builder;
        _rule3Builder = rule3Builder;
        _rule4Builder = rule4Builder;
        _rule5Builder = rule5Builder;
    }

    /// <param name="method">The method or constructor that carries <c>[MethodOverloadGenerator]</c>.</param>
    /// <param name="attribute">The resolved attribute data for rule-override arguments.</param>
    /// <param name="options">Analyzer config options for MSBuild property fallbacks.</param>
    /// <param name="attributePlacement">Where the attribute was placed (class, method, or parameter).</param>
    /// <param name="nonDelegateAttributedParamName">
    /// The name of the attributed parameter when <paramref name="attributePlacement"/> is
    /// <see cref="AttributePlacement.Parameter"/> and that parameter is not a delegate type;
    /// <see langword="null"/> otherwise.
    /// </param>
    /// <param name="attributedDelegateParamName">
    /// The name of the attributed parameter when <paramref name="attributePlacement"/> is
    /// <see cref="AttributePlacement.Parameter"/> and that parameter <em>is</em> a delegate
    /// type; <see langword="null"/> otherwise. When set, only this parameter is considered for
    /// overload generation — every other delegate parameter on the method is left untouched.
    /// </param>
    public MasterContext Build(
        IMethodSymbol method,
        AttributeData attribute,
        AnalyzerConfigOptions options,
        AttributePlacement attributePlacement,
        string? nonDelegateAttributedParamName = null,
        string? attributedDelegateParamName = null)
    {
        var allowedRules     = _allowedRulesBuilder.Build(attribute, options);
        var declaration      = _declarationBuilder.Build(method);
        var attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;

        // Always compute delegate params — needed for accurate diagnostics even when
        // some or all rules are disabled.
        var delegateParams = _delegateParamsBuilder.Build(method);

        // Parameter-level placement scopes generation to just the attributed parameter — every
        // other delegate parameter on the method is left as-is in every generated overload.
        if (attributedDelegateParamName is not null)
            delegateParams = delegateParams.Where(d => d.ParameterName == attributedDelegateParamName).ToList();

        var hasOutOrRefParams = method.Parameters.Any(
            p => p.RefKind is RefKind.Out or RefKind.Ref);

        // A single delegate parameter is still combinatorially interesting when crossed with
        // Rule 4's receiver dimension (see Rule5ContextBuilder), so an extension method only
        // needs one attributed parameter for Rule 5 to apply, instead of the usual two.
        var rule5MinimumParams = method.IsExtensionMethod ? 1 : 2;

        var anyApplicableRuleDisabled =
            (!allowedRules.AllowRule1 && delegateParams.Any(d => d.IsAsync))                    ||
            (!allowedRules.AllowRule2 && delegateParams.Any(d => d.ReturnType is not null))     ||
            (!allowedRules.AllowRule3 && delegateParams.Any(d => d.InputTypes.Count >= 2))      ||
            (!allowedRules.AllowRule4 && method.IsExtensionMethod)                              ||
            (!allowedRules.AllowRule5 && delegateParams.Count >= rule5MinimumParams);

        var rule1Contexts = allowedRules.AllowRule1 ? _rule1Builder.Build(delegateParams, allowedRules) : null;
        var rule2Contexts = allowedRules.AllowRule2 ? _rule2Builder.Build(delegateParams, allowedRules) : null;
        var rule3Contexts = allowedRules.AllowRule3 ? _rule3Builder.Build(delegateParams, allowedRules) : null;
        var rule4Context  = allowedRules.AllowRule4 ? _rule4Builder.Build(method, allowedRules) : null;
        var rule5Context  = allowedRules.AllowRule5 ? _rule5Builder.Build(delegateParams, allowedRules, rule4Context is not null) : null;

        return new MasterContext
        {
            Declaration  = declaration,
            AllowedRules = allowedRules,

            AttributePlacement             = attributePlacement,
            AttributeLocation              = attributeLocation,
            HasOutOrRefParams              = hasOutOrRefParams,
            NonDelegateAttributedParamName = nonDelegateAttributedParamName,
            AnyApplicableRuleDisabled      = anyApplicableRuleDisabled,

            ApplyRule1 = allowedRules.AllowRule1 && rule1Contexts!.Count > 0,
            ApplyRule2 = allowedRules.AllowRule2 && rule2Contexts!.Count > 0,
            ApplyRule3 = allowedRules.AllowRule3 && rule3Contexts!.Count > 0,
            ApplyRule4 = allowedRules.AllowRule4 && rule4Context is not null,
            ApplyRule5 = allowedRules.AllowRule5 && rule5Context is not null,

            Rule1Contexts = rule1Contexts,
            Rule2Contexts = rule2Contexts,
            Rule3Contexts = rule3Contexts,
            Rule4Context  = rule4Context,
            Rule5Context  = rule5Context,
        };
    }
}
