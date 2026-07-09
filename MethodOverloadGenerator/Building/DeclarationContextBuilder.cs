using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Building;

/// <summary>
/// Builds a <see cref="DeclarationContext"/> from the Roslyn symbol of the method or
/// constructor that carries <c>[MethodOverloadGenerator]</c>.
/// </summary>
internal sealed class DeclarationContextBuilder
{
    /// <param name="method">The method or constructor symbol being overloaded.</param>
    public DeclarationContext Build(IMethodSymbol method)
    {
        var containingType = method.ContainingType;

        return new DeclarationContext
        {
            Namespace           = GetNamespace(containingType),
            ClassName           = containingType.Name,
            MethodName          = method.MethodKind == MethodKind.Constructor
                                      ? containingType.Name
                                      : method.Name,
            IsConstructor       = method.MethodKind == MethodKind.Constructor,
            AccessModifier      = MapAccessibility(method.DeclaredAccessibility),
            IsStatic            = method.IsStatic,
            ClassAccessModifier = MapAccessibility(containingType.DeclaredAccessibility),
            IsStaticClass       = containingType.IsStatic,
            IsExtensionMethod   = method.IsExtensionMethod,
            IsPartialClass      = IsPartial(containingType),
            ReturnType          = method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            Parameters          = method.Parameters
                                      .Select(p => new MethodParameter
                                      {
                                          Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                          Name = p.Name,
                                      })
                                      .ToArray(),
            Documentation       = BuildDocumentation(method),
            Usings              = GetUsings(method),
            TypeParameters      = method.TypeParameters.Select(tp => tp.Name).ToArray(),
            TypeParameterConstraintClauses = method.TypeParameters
                                      .Select(BuildConstraintClause)
                                      .Where(c => c is not null)
                                      .ToArray()!,
        };
    }

    // Reconstructs the full "where T : ..." clause from the type parameter's constraint kinds
    // rather than just its constraint types, so class/struct/unmanaged/notnull/new() constraints
    // (which have no corresponding ConstraintTypes entry) aren't silently dropped.
    private static string? BuildConstraintClause(ITypeParameterSymbol tp)
    {
        var parts = new List<string>();

        if (tp.HasUnmanagedTypeConstraint)
            parts.Add("unmanaged");
        else if (tp.HasValueTypeConstraint)
            parts.Add("struct");
        else if (tp.HasReferenceTypeConstraint)
            parts.Add(tp.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        else if (tp.HasNotNullConstraint)
            parts.Add("notnull");

        parts.AddRange(tp.ConstraintTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

        if (tp.HasConstructorConstraint)
            parts.Add("new()");

        return parts.Count == 0 ? null : $"where {tp.Name} : {string.Join(", ", parts)}";
    }

    // Copies every using in scope for the method's own file (file-level and namespace-level)
    // rather than computing the minimal set actually referenced — far cheaper, and an unused
    // using is harmless (at worst an IDE hint) where a missing one is a compile error.
    private static IReadOnlyList<string> GetUsings(IMethodSymbol method)
    {
        var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is null)
            return [];

        var usings = new List<UsingDirectiveSyntax>(syntax.SyntaxTree.GetCompilationUnitRoot().Usings);

        if (syntax.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is { } namespaceNode)
            usings.AddRange(namespaceNode.Usings);

        return usings
            .Select(u => u.NormalizeWhitespace().ToFullString().Trim())
            .Distinct()
            .ToArray();
    }

    private static DocumentationComment BuildDocumentation(IMethodSymbol method)
    {
        var xml = method.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
            return DocumentationComment.None;

        XElement member;
        try
        {
            member = XElement.Parse(xml);
        }
        catch (XmlException)
        {
            return DocumentationComment.None;
        }

        return new DocumentationComment
        {
            Summary = ExtractInnerXml(member.Element("summary")),
            Remarks = ExtractInnerXml(member.Element("remarks")),
        };
    }

    // Roslyn re-indents doc comment XML to a fixed depth unrelated to the original source
    // formatting, so each line is trimmed independently rather than preserved verbatim.
    private static string? ExtractInnerXml(XElement? element)
    {
        if (element is null)
            return null;

        var inner = string.Concat(element.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
        var lines = inner.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);
        var result = string.Join("\n", lines);
        return result.Length == 0 ? null : result;
    }

    private static bool IsPartial(INamedTypeSymbol type)
        => type.DeclaringSyntaxReferences
               .Select(r => r.GetSyntax())
               .OfType<TypeDeclarationSyntax>()
               .Any(s => s.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

    private static string? GetNamespace(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace;
        return ns.IsGlobalNamespace ? null : ns.ToDisplayString();
    }

    private static AccessModifier MapAccessibility(Accessibility accessibility)
        => accessibility switch
        {
            Accessibility.Public              => AccessModifier.Public,
            Accessibility.Internal            => AccessModifier.Internal,
            Accessibility.Protected           => AccessModifier.Protected,
            Accessibility.ProtectedOrInternal => AccessModifier.ProtectedInternal,
            Accessibility.ProtectedAndInternal => AccessModifier.PrivateProtected,
            _                                  => AccessModifier.Private,
        };
}
