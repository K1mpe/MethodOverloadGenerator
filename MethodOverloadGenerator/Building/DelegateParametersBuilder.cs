using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Building;

/// <summary>
/// Extracts a <see cref="DelegateInfo"/> for every delegate-typed parameter of the method
/// that should be considered for overload generation.
///
/// This is the Roslyn-to-pure-data bridge: it inspects parameter symbols and converts
/// them into <see cref="DelegateInfo"/> records so that the rule context builders
/// (Rules 1–3 and 5) can operate without any Roslyn dependency.
/// </summary>
internal sealed class DelegateParametersBuilder
{
    /// <param name="method">The method or constructor whose parameters are inspected.</param>
    /// <returns>
    /// Ordered list of <see cref="DelegateInfo"/> records, one per delegate-typed parameter
    /// that is eligible for overload generation.  Parameters that are not delegates, or that
    /// are the <c>this</c> extension parameter, are excluded.
    /// </returns>
    public IReadOnlyList<DelegateInfo> Build(IMethodSymbol method)
    {
        var parameters  = method.Parameters;
        int startIndex  = method.IsExtensionMethod ? 1 : 0;

        var result = new List<DelegateInfo>(parameters.Length - startIndex);

        for (int i = startIndex; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.Type is INamedTypeSymbol { TypeKind: TypeKind.Delegate } delegateType)
                result.Add(BuildInfo(param.Name, delegateType.DelegateInvokeMethod!));
        }

        return result;
    }

    private static DelegateInfo BuildInfo(string parameterName, IMethodSymbol invoke)
    {
        var isAsync     = IsAsyncType(invoke.ReturnType);
        var isValueTask = IsValueTaskType(invoke.ReturnType);

        return new DelegateInfo
        {
            ParameterName = parameterName,
            InputTypes    = invoke.Parameters
                                .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                                .ToArray(),
            ReturnType    = UnwrapReturnType(invoke),
            IsAsync       = isAsync,
            IsValueTask   = isValueTask,
        };
    }

    private static string? UnwrapReturnType(IMethodSymbol invoke)
    {
        if (invoke.ReturnsVoid)
            return null;

        if (invoke.ReturnType is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
            && named.Name is "Task" or "ValueTask")
        {
            return named.IsGenericType
                ? named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                : null; // non-generic Task/ValueTask — async void-equivalent
        }

        return invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static bool IsAsyncType(ITypeSymbol type)
        => type is INamedTypeSymbol named
        && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
        && named.Name is "Task" or "ValueTask";

    private static bool IsValueTaskType(ITypeSymbol type)
        => type is INamedTypeSymbol named
        && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
        && named.Name == "ValueTask";
}
