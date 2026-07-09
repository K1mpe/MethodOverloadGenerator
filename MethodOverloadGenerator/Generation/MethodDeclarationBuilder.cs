using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Generation;

/// <summary>
/// Builds the trailing "signature + forwarding body" portion shared by every rule emitter that
/// forwards to the original method/constructor by name (Rules 1, 2, 3, and Rule 5's plain
/// combinatorial overloads — Rule 4 and Rule 5's receiver-crossed overloads never apply to
/// constructors, since a constructor can't be an extension method).
/// </summary>
internal static class MethodDeclarationBuilder
{
    /// <param name="decl">The original method/constructor's declaration context.</param>
    /// <param name="modifiers">Access modifier (+ "static" when applicable).</param>
    /// <param name="typeParams">The <c>&lt;T, ...&gt;</c> suffix, or empty.</param>
    /// <param name="signature">The parameter list, without surrounding parens.</param>
    /// <param name="whereClauses">The <c>where</c> clause suffix, or empty.</param>
    /// <param name="callArgs">Arguments to forward to the original method/constructor.</param>
    public static string Build(
        DeclarationContext decl,
        string modifiers,
        string typeParams,
        string signature,
        string whereClauses,
        string callArgs)
    {
        // A constructor can't forward with a plain method call — MethodName(args) inside a
        // constructor named the same as the class isn't self-referential chaining, it just
        // doesn't resolve. Constructors also have no return type at all.
        if (decl.IsConstructor)
            return $"    {modifiers} {decl.MethodName}{typeParams}({signature}){whereClauses}\n"
                 + $"        : this({callArgs}) {{ }}";

        return $"    {modifiers} {decl.ReturnType} {decl.MethodName}{typeParams}({signature}){whereClauses}\n"
             + $"        => {decl.MethodName}({callArgs});";
    }
}
