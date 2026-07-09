namespace MethodOverloadGenerator;

/// <summary>
/// Central source of truth for names derived from the [MethodOverloadGenerator] attribute.
/// Change <see cref="AttributeSimpleName"/> and everything else cascades automatically.
/// </summary>
internal static class GeneratorConstants
{
    /// <summary>The user-facing attribute name as written in C# without the "Attribute" suffix.</summary>
    private const string AttributeSimpleName = "MethodOverloadGenerator";

    /// <summary>The C# class name of the generated attribute.</summary>
    internal const string AttributeClassName = AttributeSimpleName + "Attribute";

    /// <summary>The fully qualified metadata name used by <c>ForAttributeWithMetadataName</c>.</summary>
    internal const string AttributeMetadataName = "MethodOverloadGenerator." + AttributeClassName;

    /// <summary>The hint name for the generated attribute source file.</summary>
    internal const string AttributeHintName = AttributeClassName + ".g.cs";

    /// <summary>The bracketed display form used in diagnostic messages: <c>[MethodOverloadGenerator]</c>.</summary>
    internal const string AttributeDisplayName = "[" + AttributeSimpleName + "]";

    /// <summary>Suffix appended to the parameter name in Rule 2 (fixed-value) overloads.</summary>
    internal const string ValueParamSuffix = "Value";

    /// <summary>The hint name for the polyfilled <c>OverloadResolutionPriorityAttribute</c> source file.</summary>
    internal const string OverloadResolutionPriorityHintName = "OverloadResolutionPriorityAttribute.g.cs";
}
