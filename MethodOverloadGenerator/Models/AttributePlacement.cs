namespace MethodOverloadGenerator.Models;

internal enum AttributePlacement
{
    /// <summary>The [MethodOverloadGenerator] attribute was placed on the class.</summary>
    Class,
    /// <summary>The [MethodOverloadGenerator] attribute was placed on the method or constructor.</summary>
    Method,
    /// <summary>The [MethodOverloadGenerator] attribute was placed on a specific parameter.</summary>
    Parameter,
}
