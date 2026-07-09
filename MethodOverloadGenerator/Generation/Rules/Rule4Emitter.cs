using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation.Rules;

/// <summary>
/// Emits extension-method Task/ValueTask receiver overloads for Rule 4.
/// Produces exactly two methods: one whose <c>this</c> parameter is
/// <c>Task&lt;<see cref="Rule4Context.ThisParameterType"/>&gt;</c>
/// and one where it is <c>ValueTask&lt;<see cref="Rule4Context.ThisParameterType"/>&gt;</c>.
/// Both overloads are <c>async</c> and <c>await</c> the incoming task before forwarding.
/// </summary>
internal sealed class Rule4Emitter
{
    public IReadOnlyList<string> Emit(Rule4Context context, DeclarationContext declaration)
        =>
        [
            EmitOne(context, declaration, "Task"),
            EmitOne(context, declaration, "ValueTask"),
        ];

    private static string EmitOne(Rule4Context ctx, DeclarationContext decl, string wrapperType)
    {
        var receiverInnerType = ctx.GenericParameterName ?? ctx.ThisParameterType;
        var receiverType      = $"{wrapperType}<{receiverInnerType}>";
        var returnType        = BuildReturnType(ctx);
        var modifiers         = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "") + " async";
        var typeParams        = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, ctx.GenericParameterName);
        var whereClauses      = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses,
                                     ctx.RequiresGenericConstraint ? $"where {ctx.GenericParameterName} : {ctx.ThisParameterType}" : null);
        var signature         = BuildSignature(decl, receiverType);
        var body              = BuildBody(ctx, decl);
        var doc               = DocCommentBuilder.Build(decl.Documentation,
            $"Generated overload: accepts the receiver as <c>{wrapperType}&lt;{receiverInnerType}&gt;</c> " +
             "and awaits it before forwarding to the original method.");
        // No delegate parameter's arity changes — only the receiver's own type — so this ranks
        // just below the original, alongside Rule 1's same-arity sync overloads.
        var priority          = OverloadPriority.Attribute(0);

        return doc
             + priority
             + $"    {modifiers} {returnType} {decl.MethodName}{typeParams}({signature}){whereClauses}\n"
             + $"        => {body};";
    }

    // -----------------------------------------------------------------------------------------
    // Return type — async methods keep their own return type; sync methods get wrapped in Task.
    // -----------------------------------------------------------------------------------------

    private static string BuildReturnType(Rule4Context ctx)
    {
        if (ctx.IsAsyncMethod) return ctx.MethodReturnType!;
        return ctx.MethodReturnType is null ? "Task" : $"Task<{ctx.MethodReturnType}>";
    }

    // -----------------------------------------------------------------------------------------
    // Body — await the receiver, then forward to the original method.
    // Async methods need a second await to unwrap the original method's Task/ValueTask.
    // -----------------------------------------------------------------------------------------

    private static string BuildBody(Rule4Context ctx, DeclarationContext decl)
    {
        var otherArgs = string.Join(", ", decl.Parameters.Skip(1).Select(p => p.Name));
        var call      = $"(await {ctx.ThisParameterName}).{decl.MethodName}({otherArgs})";
        return ctx.IsAsyncMethod ? $"await {call}" : call;
    }

    // -----------------------------------------------------------------------------------------
    // Method signature — the this parameter is wrapped, all other parameters pass through.
    // -----------------------------------------------------------------------------------------

    private static string BuildSignature(DeclarationContext decl, string receiverType)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p    = decl.Parameters[i];
            var type = i == 0 ? receiverType : p.Type;
            parts[i] = i == 0 ? $"this {type} {p.Name}" : $"{type} {p.Name}";
        }
        return string.Join(", ", parts);
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
