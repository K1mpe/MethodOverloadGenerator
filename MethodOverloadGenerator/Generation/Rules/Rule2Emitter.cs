using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation.Rules;

/// <summary>
/// Emits fixed-value-overload method declarations for Rule 2.
/// For each <see cref="Rule2Context"/>, produces one method whose delegate parameter
/// is replaced with a plain value of the delegate's return type, with a body that
/// wraps the value back into the delegate before forwarding to the original method.
/// </summary>
internal sealed class Rule2Emitter
{
    public IReadOnlyList<string> Emit(IReadOnlyList<Rule2Context> contexts, DeclarationContext declaration)
    {
        var results = new List<string>(contexts.Count);
        foreach (var ctx in contexts)
            results.Add(EmitOne(ctx.Delegate, declaration));
        return results;
    }

    private static string EmitOne(DelegateInfo d, DeclarationContext decl)
    {
        var valueName    = d.ParameterName + GeneratorConstants.ValueParamSuffix;
        var wrapExpr     = BuildWrapExpression(d, valueName);
        var modifiers    = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "");
        var typeParams   = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, null);
        var whereClauses = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses, null);
        var signature    = BuildSignature(decl, d.ParameterName, d.ReturnType!, valueName);
        var callArgs     = BuildCallArgs(decl, d.ParameterName, wrapExpr);
        var doc          = DocCommentBuilder.Build(decl.Documentation,
            $"Generated overload: accepts a fixed value instead of a delegate for <paramref name=\"{valueName}\"/>. " +
             "<br/><b>Warning:</b> the value is evaluated once, at the call site, not lazily on each invocation");
        var priority     = OverloadPriority.Attribute(OverloadPriority.ForFixedValue(d));

        return doc
             + priority
             + MethodDeclarationBuilder.Build(decl, modifiers, typeParams, signature, whereClauses, callArgs);
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — the lambda passed in place of the value-returning delegate.
    // For delegates with input types the lambda accepts (and ignores) each input, then
    // returns the fixed value.
    // -----------------------------------------------------------------------------------------

    internal static string BuildWrapExpression(DelegateInfo d, string? valueName = null)
    {
        var header = LambdaHeader(d.InputTypes);
        var value  = valueName ?? d.ParameterName;

        return (d.IsAsync, d.IsValueTask) switch
        {
            (true, false) => $"{header} => Task.FromResult({value})",
            (true, true)  => $"{header} => new ValueTask<{d.ReturnType}>({value})",
            _             => $"{header} => {value}",
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

    private static string BuildSignature(DeclarationContext decl, string delegateParamName, string valueType, string valueName)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p          = decl.Parameters[i];
            var prefix     = decl.IsExtensionMethod && i == 0 ? "this " : "";
            var isDelegate = p.Name == delegateParamName;
            var type       = isDelegate ? valueType : p.Type;
            var name       = isDelegate ? valueName  : p.Name;
            parts[i]       = $"{prefix}{type} {name}";
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
