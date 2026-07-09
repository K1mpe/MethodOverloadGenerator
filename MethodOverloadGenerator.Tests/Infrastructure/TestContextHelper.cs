using Microsoft.CodeAnalysis;
using MethodOverloadGenerator.Building;
using MethodOverloadGenerator.Building.Rules;
using MethodOverloadGenerator.Models;

namespace MethodOverloadGenerator.Tests.Infrastructure;

public static class TestContextHelper
{
    /// <summary>
    /// Builds a <see cref="MasterContext"/> directly from a C# code block.
    /// The source must contain exactly one <c>[MethodOverloadGenerator]</c> attribute placed on a
    /// class, method/constructor, or parameter.  All MSBuild rule overrides default to enabled.
    /// </summary>
    internal static MasterContext CreateMasterContext(string source)
    {
        var compilation = RoslynTestHelper.CreateCompilation(source);
        var (method, attribute, placement, nonDelegateParamName, attributedDelegateParamName) = FindAttributedMethod(compilation);

        var builder = new MasterContextBuilder(
            new DeclarationContextBuilder(),
            new AllowedRulesContextBuilder(),
            new DelegateParametersBuilder(),
            new Rule1ContextsBuilder(),
            new Rule2ContextsBuilder(),
            new Rule3ContextsBuilder(),
            new Rule4ContextBuilder(),
            new Rule5ContextBuilder());

        var ctx =  builder.Build(method, attribute, TestAnalyzerConfigOptions.Empty, placement, nonDelegateParamName, attributedDelegateParamName);
        return ctx;
    }

    private static (IMethodSymbol Method, AttributeData Attribute, AttributePlacement Placement, string? NonDelegateParamName, string? AttributedDelegateParamName)
        FindAttributedMethod(Compilation compilation)
    {
        foreach (var type in GetAllTypes(compilation.GlobalNamespace))
        {
            var classAttr = GetGeneratorAttribute(type);
            if (classAttr is not null)
            {
                var method = type.GetMembers()
                    .OfType<IMethodSymbol>()
                    .First(m => m.MethodKind is MethodKind.Ordinary or MethodKind.Constructor);
                return (method, classAttr, AttributePlacement.Class, null, null);
            }

            foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
            {
                var methodAttr = GetGeneratorAttribute(member);
                if (methodAttr is not null)
                    return (member, methodAttr, AttributePlacement.Method, null, null);

                foreach (var param in member.Parameters)
                {
                    var paramAttr = GetGeneratorAttribute(param);
                    if (paramAttr is not null)
                    {
                        var isDelegateParam = param.Type.TypeKind == TypeKind.Delegate;
                        var nonDelegateParamName = isDelegateParam ? null : param.Name;
                        var attributedDelegateParamName = isDelegateParam ? param.Name : null;
                        return (member, paramAttr, AttributePlacement.Parameter, nonDelegateParamName, attributedDelegateParamName);
                    }
                }
            }
        }

        throw new InvalidOperationException("No [MethodOverloadGenerator] attribute found in source.");
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
                foreach (var type in GetAllTypes(childNs))
                    yield return type;
            else if (member is INamedTypeSymbol type)
                yield return type;
        }
    }

    private static AttributeData? GetGeneratorAttribute(ISymbol symbol)
        => symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == GeneratorConstants.AttributeClassName);

    public static void NormalisedContains(string output, string expected)
    {
        var normalizedOutput = output.Replace("\r\n", "").Replace("\n", "").Replace(" ", "").Trim();
        var normalizedExpected = expected.Replace("\r\n", "").Replace("\n", "").Replace(" ", "").Trim();
        if (!normalizedOutput.Contains(normalizedExpected))
            throw new InvalidOperationException($"Output did not contain expected.\n\nExpected:\n{normalizedExpected}\n\nActual:\n{normalizedOutput}");
    }

    public static void NormalisedContains(IEnumerable<string> outputLines, params string[] expected)
    {
        var outputs = outputLines.Select(l => l.Replace("\r\n", "").Replace("\n", "").Replace(" ", "").Trim()).ToList();
        foreach (var e in expected)
        {
            var normalisedExpected = e.Replace("\r\n", "").Replace("\n", "").Replace(" ", "").Trim();

            bool match = false;
            foreach(var output in outputs)
            {
                if (output.Contains(normalisedExpected))
                {
                    match = true;
                    continue;
                }
            }
            if(!match)
                throw new InvalidOperationException($"Output did not contain expected.Actual:\n{normalisedExpected}");
        }
    }
}
