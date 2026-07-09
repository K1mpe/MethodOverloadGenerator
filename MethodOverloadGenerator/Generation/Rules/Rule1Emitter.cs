using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation.Rules;

/// <summary>
/// Emits sync-overload method declarations for Rule 1.
/// For each <see cref="Rule1Context"/>, produces one method whose delegate parameter
/// is replaced with its synchronous equivalent (<c>Func&lt;T&gt;</c> → <c>T</c>,
/// <c>Func&lt;Task&gt;</c> → <c>Action</c>), with a body that wraps the value back
/// into a completed task before forwarding to the original method.
/// </summary>
internal sealed class Rule1Emitter
{
    public IReadOnlyList<string> Emit(IReadOnlyList<Rule1Context> contexts, DeclarationContext declaration)
    {
        var results = new List<string>(contexts.Count);
        foreach (var ctx in contexts)
            results.Add(EmitOne(ctx.Delegate, declaration));
        return results;
    }

    private static string EmitOne(DelegateInfo d, DeclarationContext decl)
    {
        var syncType     = BuildSyncType(d);
        var wrapExpr     = BuildWrapExpression(d);
        var modifiers    = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "");
        var typeParams   = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, null);
        var whereClauses = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses, null);
        var signature    = BuildSignature(decl, d.ParameterName, syncType);
        var callArgs     = BuildCallArgs(decl, d.ParameterName, wrapExpr);
        var doc          = DocCommentBuilder.Build(decl.Documentation,
            $"Generated overload: accepts a synchronous replacement for <paramref name=\"{d.ParameterName}\"/> " +
             "and wraps it in a completed task before forwarding to the original method.");
        var priority     = OverloadPriority.Attribute(OverloadPriority.ForSync(d));

        return doc
             + priority
             + MethodDeclarationBuilder.Build(decl, modifiers, typeParams, signature, whereClauses, callArgs);
    }

    // -----------------------------------------------------------------------------------------
    // Sync parameter type
    // -----------------------------------------------------------------------------------------

    internal static string BuildSyncType(DelegateInfo d)
    {
        if (d.ReturnType is null)
        {
            if (d.InputTypes.Count == 0) return "Action";
            return "Action<" + string.Join(", ", d.InputTypes) + ">";
        }
        if (d.InputTypes.Count == 0) return "Func<" + d.ReturnType + ">";
        return "Func<" + string.Join(", ", d.InputTypes) + ", " + d.ReturnType + ">";
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — the lambda passed in place of the async delegate
    // -----------------------------------------------------------------------------------------

    internal static string BuildWrapExpression(DelegateInfo d)
    {
        var header   = LambdaHeader(d.InputTypes);
        var callArgs = string.Join(", ", Enumerable.Range(0, d.InputTypes.Count).Select(i => $"p{i}"));
        var invoke   = d.InputTypes.Count == 0 ? $"{d.ParameterName}()" : $"{d.ParameterName}({callArgs})";

        return (d.ReturnType, d.IsValueTask) switch
        {
            (not null, false) => $"{header} => Task.FromResult({invoke})",
            (not null, true)  => $"{header} => new ValueTask<{d.ReturnType}>({invoke})",
            (null,    false)  => $"{header} => {{ {invoke}; return Task.CompletedTask; }}",
            (null,    true)   => $"{header} => {{ {invoke}; return default; }}",
        };
    }

    private static string LambdaHeader(IReadOnlyList<string> inputTypes)
    {
        if (inputTypes.Count == 0) return "()";
        return "(" + string.Join(", ", inputTypes.Select((t, i) => $"{t} p{i}")) + ")";
    }

    // -----------------------------------------------------------------------------------------
    // Method signature and call-site argument list
    // -----------------------------------------------------------------------------------------

    private static string BuildSignature(DeclarationContext decl, string delegateParamName, string syncType)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p      = decl.Parameters[i];
            var prefix = decl.IsExtensionMethod && i == 0 ? "this " : "";
            var type   = p.Name == delegateParamName ? syncType : p.Type;
            parts[i]   = $"{prefix}{type} {p.Name}";
        }
        return string.Join(", ", parts);
    }

    private static string BuildCallArgs(DeclarationContext decl, string delegateParamName, string wrapExpr)
    {
        var args = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p   = decl.Parameters[i];
            args[i] = p.Name == delegateParamName ? wrapExpr : p.Name;
        }
        return string.Join(", ", args);
    }

    // -----------------------------------------------------------------------------------------
    // Access modifier → keyword
    // -----------------------------------------------------------------------------------------

    private static string AccessKeyword(AccessModifier m) => m switch
    {
        AccessModifier.Public            => "public",
        AccessModifier.Internal          => "internal",
        AccessModifier.Protected         => "protected",
        AccessModifier.ProtectedInternal => "protected internal",
        AccessModifier.PrivateProtected  => "private protected",
        _                                => "private",
    };
}
