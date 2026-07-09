using MethodOverloadGenerator.Models;
using MethodOverloadGenerator.Models.Rules;

namespace MethodOverloadGenerator.Generation.Rules;

/// <summary>
/// Emits combinatorial overloads for Rule 5.
/// Computes the cross-product of each attributed parameter's individual variants
/// (derived from Rules 1–3) and produces one method per combination where at least
/// two parameters are substituted simultaneously, calling the original method directly.
/// Single-parameter substitutions (depth-1) are already handled by Rules 1–3 and are
/// excluded here to avoid duplicate method signatures.
/// </summary>
/// <remarks>
/// When the method is also eligible for Rule 4 (extension-method <c>Task&lt;T&gt;</c> /
/// <c>ValueTask&lt;T&gt;</c> receiver overloads), the receiver becomes an additional
/// dimension in the combination: for every parameter combination with at least one
/// substituted parameter, an extra async overload is emitted for each receiver wrapper
/// (<c>Task&lt;T&gt;</c> and <c>ValueTask&lt;T&gt;</c>), mirroring <see cref="Rule4Emitter"/>'s
/// await-and-forward body. Receiver-only substitutions (no parameter changed) are already
/// covered by <see cref="Rule4Emitter"/> and are excluded here.
/// </remarks>
internal sealed class Rule5Emitter
{
    private record struct ParameterVariant(string Type, string WrapExpression, int Reduction);

    private static readonly string[] ReceiverWrapperKinds = ["Task", "ValueTask"];

    public IReadOnlyList<string> Emit(
        Rule5Context context,
        DeclarationContext declaration,
        AllowedRulesContext allowedRules,
        Rule4Context? rule4Context = null)
    {
        var variantsPerParam = context.AttributedParameters
            .Select(d => GetVariants(d, allowedRules))
            .ToList();

        var combos = CartesianProduct(variantsPerParam);

        var results = new List<string>();
        foreach (var combo in combos)
        {
            if (combo.Count(v => v is not null) < 2) continue;
            results.Add(EmitCombination(context.AttributedParameters, combo, declaration));
        }

        if (rule4Context is not null)
        {
            foreach (var combo in combos)
            {
                if (combo.Count(v => v is not null) < 1) continue;
                foreach (var wrapperKind in ReceiverWrapperKinds)
                    results.Add(EmitReceiverCombination(
                        context.AttributedParameters, combo, declaration, rule4Context, wrapperKind));
            }
        }

        return results;
    }

    // -----------------------------------------------------------------------------------------
    // Per-parameter variant enumeration
    // -----------------------------------------------------------------------------------------

    private static IReadOnlyList<ParameterVariant?> GetVariants(
        DelegateInfo d, AllowedRulesContext allowed)
    {
        var variants = new List<ParameterVariant?> { null }; // null = original (no substitution)

        if (allowed.AllowRule1 && d.IsAsync)
            variants.Add(new ParameterVariant(
                Rule1Emitter.BuildSyncType(d),
                Rule1Emitter.BuildWrapExpression(d),
                OverloadPriority.ForSync(d)));

        if (allowed.AllowRule2 && d.ReturnType is not null)
            variants.Add(new ParameterVariant(
                d.ReturnType,
                Rule2Emitter.BuildWrapExpression(d),
                OverloadPriority.ForFixedValue(d)));

        if (allowed.AllowRule3 && d.InputTypes.Count >= 1)
        {
            // Async delegates get both a reduced-arity form that stays async (async-drop) and
            // one that also converts to sync (fully-sync) — the same two forms Rule3Emitter
            // itself emits standalone. Non-async delegates only have the plain-reduced form.
            for (int k = d.InputTypes.Count - 1; k >= 0; k--)
            {
                if (d.IsAsync)
                    variants.Add(new ParameterVariant(
                        Rule3Emitter.BuildAsyncDropType(d, k),
                        Rule3Emitter.BuildWrapExpression(d, k),
                        OverloadPriority.ForReducedArity(d, k, preserveAsync: true)));

                variants.Add(new ParameterVariant(
                    Rule3Emitter.BuildSyncReducedType(d, k),
                    d.IsAsync ? Rule3Emitter.BuildSyncDropWrapExpression(d, k) : Rule3Emitter.BuildWrapExpression(d, k),
                    OverloadPriority.ForReducedArity(d, k, preserveAsync: false)));
            }
        }

        return variants;
    }

    // -----------------------------------------------------------------------------------------
    // Combination emission
    // -----------------------------------------------------------------------------------------

    private static string EmitCombination(
        IReadOnlyList<DelegateInfo> attributed,
        IReadOnlyList<ParameterVariant?> combo,
        DeclarationContext decl)
    {
        var typeSubst = new Dictionary<string, string>();
        var wrapSubst = new Dictionary<string, string>();

        for (int i = 0; i < attributed.Count; i++)
        {
            if (combo[i] is { } variant)
            {
                typeSubst[attributed[i].ParameterName] = variant.Type;
                wrapSubst[attributed[i].ParameterName] = variant.WrapExpression;
            }
        }

        var modifiers    = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "");
        var typeParams   = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, null);
        var whereClauses = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses, null);
        var signature    = BuildSignature(decl, typeSubst);
        var callArgs     = BuildCallArgs(decl, wrapSubst);
        var doc          = DocCommentBuilder.Build(decl.Documentation,
            $"Generated combinatorial overload: substitutes multiple delegate parameters at once " +
            $"({SubstitutedParamRefs(typeSubst.Keys)}). See the individual per-parameter overloads " +
             "for details on each substitution.");
        var priority     = OverloadPriority.Attribute(combo.Sum(v => v?.Reduction ?? 0));

        return doc
             + priority
             + MethodDeclarationBuilder.Build(decl, modifiers, typeParams, signature, whereClauses, callArgs);
    }

    // -----------------------------------------------------------------------------------------
    // Combination emission — Rule 4 (Task<T>/ValueTask<T> receiver) crossed with a
    // parameter-substitution combination. Mirrors Rule4Emitter's await-and-forward shape,
    // but forwards substituted arguments via their wrap expressions instead of plain names.
    // -----------------------------------------------------------------------------------------

    private static string EmitReceiverCombination(
        IReadOnlyList<DelegateInfo> attributed,
        IReadOnlyList<ParameterVariant?> combo,
        DeclarationContext decl,
        Rule4Context rule4,
        string wrapperKind)
    {
        var typeSubst = new Dictionary<string, string>();
        var wrapSubst = new Dictionary<string, string>();

        for (int i = 0; i < attributed.Count; i++)
        {
            if (combo[i] is { } variant)
            {
                typeSubst[attributed[i].ParameterName] = variant.Type;
                wrapSubst[attributed[i].ParameterName] = variant.WrapExpression;
            }
        }

        var receiverInnerType = rule4.GenericParameterName ?? rule4.ThisParameterType;
        var receiverType      = $"{wrapperKind}<{receiverInnerType}>";
        var returnType        = BuildReceiverReturnType(rule4);
        var modifiers         = AccessKeyword(decl.AccessModifier) + (decl.IsStatic ? " static" : "") + " async";
        var typeParams        = GenericSignatureHelper.BuildTypeParameterList(decl.TypeParameters, rule4.GenericParameterName);
        var whereClauses      = GenericSignatureHelper.BuildWhereClauses(decl.TypeParameterConstraintClauses,
                                     rule4.RequiresGenericConstraint ? $"where {rule4.GenericParameterName} : {rule4.ThisParameterType}" : null);
        var signature         = BuildReceiverSignature(decl, receiverType, typeSubst);
        var otherArgs         = BuildReceiverCallArgs(decl, wrapSubst);
        var call              = $"(await {rule4.ThisParameterName}).{decl.MethodName}({otherArgs})";
        var body              = rule4.IsAsyncMethod ? $"await {call}" : call;
        var doc               = DocCommentBuilder.Build(decl.Documentation,
            $"Generated combinatorial overload: accepts the receiver as <c>{wrapperKind}&lt;{receiverInnerType}&gt;</c> " +
            $"and substitutes {SubstitutedParamRefs(typeSubst.Keys)}. See the individual per-parameter and receiver " +
             "overloads for details on each substitution.");
        var priority          = OverloadPriority.Attribute(combo.Sum(v => v?.Reduction ?? 0));

        return doc
             + priority
             + $"    {modifiers} {returnType} {decl.MethodName}{typeParams}({signature}){whereClauses}\n"
             + $"        => {body};";
    }

    private static string BuildReceiverReturnType(Rule4Context rule4)
    {
        if (rule4.IsAsyncMethod) return rule4.MethodReturnType!;
        return rule4.MethodReturnType is null ? "Task" : $"Task<{rule4.MethodReturnType}>";
    }

    private static string BuildReceiverSignature(
        DeclarationContext decl,
        string receiverType,
        IReadOnlyDictionary<string, string> typeSubst)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p = decl.Parameters[i];
            if (i == 0)
            {
                parts[i] = $"this {receiverType} {p.Name}";
                continue;
            }
            var type = typeSubst.TryGetValue(p.Name, out var t) ? t : p.Type;
            parts[i] = $"{type} {p.Name}";
        }
        return string.Join(", ", parts);
    }

    private static string BuildReceiverCallArgs(
        DeclarationContext decl,
        IReadOnlyDictionary<string, string> wrapSubst)
    {
        var args = new string[decl.Parameters.Count - 1];
        for (int i = 1; i < decl.Parameters.Count; i++)
        {
            var p        = decl.Parameters[i];
            args[i - 1]  = wrapSubst.TryGetValue(p.Name, out var w) ? w : p.Name;
        }
        return string.Join(", ", args);
    }

    // -----------------------------------------------------------------------------------------
    // Method signature and call-site argument list (multi-substitution variants)
    // -----------------------------------------------------------------------------------------

    private static string BuildSignature(
        DeclarationContext decl,
        IReadOnlyDictionary<string, string> typeSubst)
    {
        var parts = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p      = decl.Parameters[i];
            var prefix = decl.IsExtensionMethod && i == 0 ? "this " : "";
            var type   = typeSubst.TryGetValue(p.Name, out var t) ? t : p.Type;
            parts[i]   = $"{prefix}{type} {p.Name}";
        }
        return string.Join(", ", parts);
    }

    private static string BuildCallArgs(
        DeclarationContext decl,
        IReadOnlyDictionary<string, string> wrapSubst)
    {
        var args = new string[decl.Parameters.Count];
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            var p   = decl.Parameters[i];
            args[i] = wrapSubst.TryGetValue(p.Name, out var w) ? w : p.Name;
        }
        return string.Join(", ", args);
    }

    // -----------------------------------------------------------------------------------------
    // Doc comment helper — renders the substituted parameter names as <paramref> tags
    // -----------------------------------------------------------------------------------------

    private static string SubstitutedParamRefs(IEnumerable<string> paramNames)
        => string.Join(", ", paramNames.Select(n => $"<paramref name=\"{n}\"/>"));

    // -----------------------------------------------------------------------------------------
    // Cartesian product over per-parameter variant lists
    // -----------------------------------------------------------------------------------------

    private static List<ParameterVariant?[]> CartesianProduct(
        IReadOnlyList<IReadOnlyList<ParameterVariant?>> sets)
    {
        var result = new List<ParameterVariant?[]> { Array.Empty<ParameterVariant?>() };
        foreach (var set in sets)
        {
            var next = new List<ParameterVariant?[]>(result.Count * set.Count);
            foreach (var prefix in result)
                foreach (var item in set)
                {
                    var combo = new ParameterVariant?[prefix.Length + 1];
                    prefix.CopyTo(combo, 0);
                    combo[combo.Length - 1] = item;
                    next.Add(combo);
                }
            result = next;
        }
        return result;
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
