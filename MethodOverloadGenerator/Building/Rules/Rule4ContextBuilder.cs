using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building.Rules;

/// <summary>
/// Builds the <see cref="Rule4Context"/> for the extension-method Task/ValueTask receiver rule.
/// Returns <see langword="null"/> when the method is not an extension method or when the rule
/// is not allowed by <paramref name="allowedRules"/>.
/// </summary>
internal sealed class Rule4ContextBuilder
{
    public Rule4Context? Build(IMethodSymbol method, AllowedRulesContext allowedRules)
    {
        if (!allowedRules.AllowRule4 || !method.IsExtensionMethod)
            return null;

        var thisParam        = method.Parameters[0];
        var thisParameterType = thisParam.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var (genericParameterName, requiresConstraint) = BuildGenericParameter(thisParam.Type, thisParameterType);

        return new Rule4Context
        {
            ThisParameterType         = thisParameterType,
            ThisParameterName         = thisParam.Name,
            MethodReturnType          = method.ReturnsVoid
                                            ? null
                                            : method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IsAsyncMethod             = IsAsyncReturnType(method.ReturnType),
            GenericParameterName      = genericParameterName,
            RequiresGenericConstraint = requiresConstraint,
        };
    }

    private static bool IsAsyncReturnType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol named) return false;
        return named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
            && named.Name is "Task" or "ValueTask";
    }

    // -----------------------------------------------------------------------------------------
    // Generic receiver parameter — see remarks on Rule4Context.GenericParameterName.
    // Task<T> is not covariant, so an interface/non-sealed-class receiver needs a fresh type
    // parameter constrained to it (or reuse of an existing one) to accept any subtype's Task<>.
    // -----------------------------------------------------------------------------------------

    private static (string? Name, bool RequiresConstraint) BuildGenericParameter(ITypeSymbol thisType, string thisParameterType)
    {
        if (thisType.TypeKind == TypeKind.TypeParameter)
            return (thisType.Name, false);

        var canHaveSubtypes = thisType.TypeKind switch
        {
            TypeKind.Interface => true,
            TypeKind.Class      => thisType is INamedTypeSymbol { IsSealed: false },
            _                   => false,
        };

        return canHaveSubtypes
            ? (SynthesizeTypeParameterName(thisParameterType), true)
            : (null, false);
    }

    private static string SynthesizeTypeParameterName(string typeDisplay)
    {
        var name = typeDisplay;

        var genericStart = name.IndexOf('<');
        if (genericStart >= 0) name = name.Substring(0, genericStart);
        name = name.TrimEnd('?');

        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0) name = name.Substring(lastDot + 1);

        if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);

        return "T" + name;
    }
}
