using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation.Rules;

/// <summary>
/// Emits trailing-parameter-variant method declarations for Rule 3.
/// For each <see cref="Rule3Context"/>, produces exactly one method at the arity
/// specified by <see cref="Rule3Context.TargetInputCount"/>:
/// <list type="bullet">
///   <item><see cref="Rule3Context.PreserveAsync"/> — async-drop form (keeps the async return type, k inputs)</item>
///   <item>Otherwise — fully-sync form (drops both the trailing inputs and the async wrapper, or
///         for an already-non-async delegate, just the plain-reduced form at k inputs)</item>
/// </list>
/// </summary>
internal sealed class Rule3Emitter
{
    public IReadOnlyList<string> Emit(IReadOnlyList<Rule3Context> contexts, DeclarationContext declaration)
    {
        var results = new List<string>(contexts.Count);
        foreach (var ctx in contexts)
            results.Add(EmitOne(ctx.Delegate, ctx.TargetInputCount, ctx.PreserveAsync, declaration));
        return results;
    }

    private static string EmitOne(DelegateInfo d, int k, bool preserveAsync, DeclarationContext decl)
    {
        // Converting to sync at a reduced arity needs the return value wrapped back into a task
        // only when the original delegate was async to begin with — an already-sync delegate's
        // plain-reduced form needs no wrapping at all.
        var convertingToSync = d.IsAsync && !preserveAsync;

        var paramType    = preserveAsync ? BuildAsyncDropType(d, k) : BuildSyncReducedType(d, k);
        var wrapExpr     = convertingToSync ? BuildSyncDropWrapExpression(d, k) : BuildWrapExpression(d, k);
        var modifiers    = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "");
        var typeParams   = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, null);
        var whereClauses = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses, null);
        var signature    = BuildSignature(decl, d.ParameterName, paramType);
        var callArgs     = BuildCallArgs(decl, d.ParameterName, wrapExpr);
        var note         = convertingToSync
            ? $"({k} instead of {d.InputTypes.Count}) and converts it to a synchronous return."
            : $"({k} instead of {d.InputTypes.Count}) — the trailing parameter(s) are simply not passed to it.";
        var doc          = DocCommentBuilder.Build(decl.Documentation,
            $"Generated overload: accepts a delegate for <paramref name=\"{d.ParameterName}\"/> with fewer parameters " + note);
        var priority     = OverloadPriority.Attribute(OverloadPriority.ForReducedArity(d, k, preserveAsync));

        return doc
             + priority
             + MethodDeclarationBuilder.Build(decl, modifiers, typeParams, signature, whereClauses, callArgs);
    }

    // -----------------------------------------------------------------------------------------
    // Parameter types — exposed for Rule 5 to build cross-product combinations
    // -----------------------------------------------------------------------------------------

    internal static string BuildAsyncDropType(DelegateInfo d, int k)
    {
        var asyncReturn = (d.ReturnType, d.IsValueTask) switch
        {
            (not null, true)  => $"ValueTask<{d.ReturnType}>",
            (not null, false) => $"Task<{d.ReturnType}>",
            (null, true)      => "ValueTask",
            (null, false)     => "Task",
        };

        var parts = d.InputTypes.Take(k).Append(asyncReturn);
        return "Func<" + string.Join(", ", parts) + ">";
    }

    internal static string BuildSyncReducedType(DelegateInfo d, int k)
    {
        var inputs = d.InputTypes.Take(k).ToList();

        if (d.ReturnType is not null)
        {
            if (inputs.Count == 0) return $"Func<{d.ReturnType}>";
            return "Func<" + string.Join(", ", inputs) + $", {d.ReturnType}>";
        }

        if (inputs.Count == 0) return "Action";
        return "Action<" + string.Join(", ", inputs) + ">";
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — full-arity lambda forwarding k arguments to the reduced delegate.
    // Exposed for Rule 5 to use when building combination call args.
    // -----------------------------------------------------------------------------------------

    internal static string BuildWrapExpression(DelegateInfo d, int k)
    {
        var header = FullArityLambdaHeader(d.InputTypes.Count);
        var invoke = InnerInvocation(d.ParameterName, k);
        return $"{header} => {invoke}";
    }

    // -----------------------------------------------------------------------------------------
    // Wrap expression — fully-sync variant. Same full-arity lambda and reduced-arity invocation
    // as BuildWrapExpression, but the reduced delegate now returns a plain value instead of a
    // task, so the call must be wrapped back into a task to match the original async signature
    // (mirrors Rule1Emitter.BuildWrapExpression's four-way switch). Only meaningful when the
    // original delegate is async — exposed for Rule 5 to combine with another parameter's
    // substitution.
    // -----------------------------------------------------------------------------------------

    internal static string BuildSyncDropWrapExpression(DelegateInfo d, int k)
    {
        var header = FullArityLambdaHeader(d.InputTypes.Count);
        var invoke = InnerInvocation(d.ParameterName, k);

        return (d.ReturnType, d.IsValueTask) switch
        {
            (not null, false) => $"{header} => Task.FromResult({invoke})",
            (not null, true)  => $"{header} => new ValueTask<{d.ReturnType}>({invoke})",
            (null,    false)  => $"{header} => {{ {invoke}; return Task.CompletedTask; }}",
            (null,    true)   => $"{header} => {{ {invoke}; return default; }}",
        };
    }

    private static string FullArityLambdaHeader(int n)
        => "(" + string.Join(", ", Enumerable.Range(0, n).Select(i => ((char)('a' + i)).ToString())) + ")";

    private static string InnerInvocation(string paramName, int k)
    {
        if (k == 0) return $"{paramName}()";
        return paramName + "(" + string.Join(", ", Enumerable.Range(0, k).Select(i => ((char)('a' + i)).ToString())) + ")";
    }

    // -----------------------------------------------------------------------------------------
    // Method signature and call-site argument list
    // -----------------------------------------------------------------------------------------

    private static string BuildSignature(DeclarationContext decl, string delegateParamName, string newType)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p      = decl.Parameters[i];
            var prefix = decl.IsExtensionMethod && i == 0 ? "this " : "";
            var type   = p.Name == delegateParamName ? newType : p.Type;
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
