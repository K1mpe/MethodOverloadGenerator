namespace MethodOverloadGenerator.Models;

/// <summary>
/// Captures the structural properties of the containing class and the target method that are
/// needed to emit the generated partial class shell and overload signatures correctly —
/// namespace, class name, method name, access modifier, static, and whether it is an
/// extension method.
/// </summary>
internal sealed record DeclarationContext
{
    /// <summary>
    /// Fully qualified namespace of the containing class (e.g. <c>"Animal.Services"</c>),
    /// or <see langword="null"/> for the global namespace.
    /// </summary>
    public required string? Namespace { get; init; }

    /// <summary>Name of the containing class (e.g. <c>"AnimalShelter"</c>).</summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Name of the method or constructor being overloaded.
    /// For constructors this equals <see cref="ClassName"/>.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Whether this is a constructor rather than an ordinary method. Constructor overloads
    /// can't forward with a normal method call (<c>MethodName(args)</c> inside a constructor
    /// named the same as the class would just be nonsense, not self-referential chaining) —
    /// they need a <c>: this(args)</c> constructor initializer instead, and have no return type.
    /// </summary>
    public required bool IsConstructor { get; init; }

    /// <summary>Access modifier of the method / constructor.</summary>
    public required AccessModifier AccessModifier { get; init; }

    /// <summary>Whether the method / constructor is declared <c>static</c>.</summary>
    public required bool IsStatic { get; init; }

    /// <summary>
    /// Access modifier of the containing class itself — distinct from <see cref="AccessModifier"/>,
    /// which is the method's. Reproduced on the generated partial-class shell so it matches the
    /// original declaration explicitly, rather than relying on the compiler's cross-part modifier
    /// inference for partial types.
    /// </summary>
    public required AccessModifier ClassAccessModifier { get; init; }

    /// <summary>
    /// Whether the containing class itself is declared <c>static</c> — distinct from
    /// <see cref="IsStatic"/>, which is the method's. Always <see langword="true"/> for the
    /// class that declares extension methods.
    /// </summary>
    public required bool IsStaticClass { get; init; }

    /// <summary>
    /// Whether the method is an extension method (first parameter uses <c>this</c>).
    /// Always <see langword="false"/> for constructors.
    /// Rule 4 only applies when this is <see langword="true"/>.
    /// </summary>
    public required bool IsExtensionMethod { get; init; }

    /// <summary>
    /// Whether the containing class is declared <c>partial</c>.
    /// Overload generation requires a partial class so the generator can add a second declaration.
    /// </summary>
    public required bool IsPartialClass { get; init; }

    /// <summary>
    /// Return type of the method as a minimally-qualified source string
    /// (e.g. <c>"Task&lt;Dog&gt;"</c>, <c>"void"</c>).
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// All parameters of the method in declaration order.
    /// For extension methods the first entry is the <c>this</c> parameter.
    /// </summary>
    public required IReadOnlyList<MethodParameter> Parameters { get; init; }

    /// <summary>
    /// The original method's <c>&lt;summary&gt;</c> / <c>&lt;remarks&gt;</c> doc comment, so
    /// rule emitters can reproduce the summary and extend the remarks on each generated overload.
    /// </summary>
    public required DocumentationComment Documentation { get; init; }

    /// <summary>
    /// Every <c>using</c> directive in scope for the file that declares the containing class
    /// (file-level and namespace-level), rendered as complete source lines (e.g. <c>"using
    /// Animal.Interfaces;"</c>). Reproduced verbatim in the generated file so that types
    /// referenced only via a using — rather than by their own namespace — still resolve
    /// (e.g. a delegate parameter typed <c>IAnimal</c>). Copying the whole set is far cheaper
    /// than computing the minimal set actually required, and using directives are harmless
    /// when unused (at worst an "unused using" hint).
    /// </summary>
    public required IReadOnlyList<string> Usings { get; init; }

    /// <summary>
    /// Names of the method's own generic type parameters, in declaration order (e.g.
    /// <c>["T"]</c> for <c>Eat&lt;T&gt;(...)</c>). Empty for non-generic methods. Every
    /// generated overload must redeclare the same type parameter list, since it forwards to
    /// the original method by name and any of them may still appear in the remaining
    /// (non-substituted) parameters.
    /// </summary>
    public required IReadOnlyList<string> TypeParameters { get; init; }

    /// <summary>
    /// Full <c>where</c> constraint clauses for <see cref="TypeParameters"/> — one entry per
    /// constrained type parameter (e.g. <c>["where T : ICarnivore"]</c>), covering interface /
    /// base-class constraints as well as <c>class</c>, <c>struct</c>, <c>unmanaged</c>,
    /// <c>notnull</c>, and <c>new()</c>. Empty when no type parameter has a constraint.
    /// </summary>
    public required IReadOnlyList<string> TypeParameterConstraintClauses { get; init; }
}
