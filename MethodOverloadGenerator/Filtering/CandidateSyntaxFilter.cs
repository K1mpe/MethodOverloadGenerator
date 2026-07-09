using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MethodOverloadGenerator.Filtering;

/// <summary>
/// Fast syntactic guard used as the predicate for
/// <see cref="SyntaxValueProvider.ForAttributeWithMetadataName"/>.
///
/// Roslyn's <c>ForAttributeWithMetadataName</c> already handles the semantic attribute
/// matching (metadata name lookup + symbol resolution), so this predicate only needs to
/// confirm the <em>node type</em> is one the generator can act on.  It runs on every syntax
/// change and must not access the semantic model.
/// </summary>
internal static class CandidateSyntaxFilter
{
    /// <summary>
    /// Returns <see langword="true"/> for the four node kinds that can legally carry
    /// <c>[MethodOverloadGenerator]</c>: class declarations, method declarations,
    /// constructor declarations, and parameters.
    /// </summary>
    internal static bool IsSupportedNode(SyntaxNode node, CancellationToken _) => node is
        ClassDeclarationSyntax       or
        MethodDeclarationSyntax      or
        ConstructorDeclarationSyntax or
        ParameterSyntax;
}
