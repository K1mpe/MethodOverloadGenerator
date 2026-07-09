using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MethodOverloadGenerator.Emission;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Building;

/// <summary>
/// Builds an <see cref="AllowedRulesContext"/> by resolving the three-level priority chain
/// for each rule: attribute parameter → MSBuild property → built-in default (enabled).
/// </summary>
internal sealed class AllowedRulesContextBuilder
{
    // MSBuild property keys — values are the strings "true" or "false".
    private const string KeySyncOverloads              = "build_property.MethodOverloadGenerator_SyncOverloads";
    private const string KeyValueOverloads             = "build_property.MethodOverloadGenerator_ValueOverloads";
    private const string KeyTrailingParameterOverloads = "build_property.MethodOverloadGenerator_TrailingParameterOverloads";
    private const string KeyTaskReceiverOverloads      = "build_property.MethodOverloadGenerator_TaskReceiverOverloads";
    private const string KeyCombinatorialOverloads     = "build_property.MethodOverloadGenerator_CombinatorialOverloads";

    public AllowedRulesContext Build(AttributeData attribute, AnalyzerConfigOptions options)
        => new()
        {
            AllowRule1 = Resolve(attribute, argIndex: 0, options, KeySyncOverloads),
            AllowRule2 = Resolve(attribute, argIndex: 1, options, KeyValueOverloads),
            AllowRule3 = Resolve(attribute, argIndex: 2, options, KeyTrailingParameterOverloads),
            AllowRule4 = Resolve(attribute, argIndex: 3, options, KeyTaskReceiverOverloads),
            AllowRule5 = Resolve(attribute, argIndex: 4, options, KeyCombinatorialOverloads),
        };

    private static bool Resolve(AttributeData attribute, int argIndex, AnalyzerConfigOptions options, string msBuildKey)
    {
        var ruleOverride = argIndex < attribute.ConstructorArguments.Length
            ? (RuleOverride)(int)attribute.ConstructorArguments[argIndex].Value!
            : RuleOverride.Default;

        return ruleOverride switch
        {
            RuleOverride.Enable  => true,
            RuleOverride.Disable => false,
            _                    => ReadMsBuild(options, msBuildKey),
        };
    }

    // Returns true (enabled) unless the property is explicitly set to "false".
    private static bool ReadMsBuild(AnalyzerConfigOptions options, string key)
        => !options.TryGetValue(key, out var value)
        || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
