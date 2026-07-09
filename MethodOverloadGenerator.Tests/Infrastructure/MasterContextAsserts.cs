namespace MethodOverloadGenerator.Tests.Infrastructure;

internal static class TestContextHelperExtensions
{
    internal static MasterContextAsserts Assert(this MasterContext masterContext)
        => new(masterContext);
}

internal class MasterContextAsserts
{
    private readonly MasterContext _ctx;

    public MasterContextAsserts(MasterContext masterContext) => _ctx = masterContext;

    // -----------------------------------------------------------------------------------------
    // Placement & diagnostic
    // -----------------------------------------------------------------------------------------

    public MasterContextAsserts HasAttributePlacement(AttributePlacement expected)
    {
        Assert.Equal(expected, _ctx.AttributePlacement);
        return this;
    }

    public MasterContextAsserts HasNonDelegateAttributedParamName(string? expected)
    {
        Assert.Equal(expected, _ctx.NonDelegateAttributedParamName);
        return this;
    }

    public MasterContextAsserts HasNoOutOrRefParams()
    {
        Assert.False(_ctx.HasOutOrRefParams);
        return this;
    }

    public MasterContextAsserts HasNoApplicableRuleDisabled()
    {
        Assert.False(_ctx.AnyApplicableRuleDisabled);
        return this;
    }

    // -----------------------------------------------------------------------------------------
    // Declaration
    // -----------------------------------------------------------------------------------------

    public MasterContextAsserts HasDeclaration(string? ns, string className, string methodName, string returnType)
    {
        Assert.Equal(ns,         _ctx.Declaration.Namespace);
        Assert.Equal(className,  _ctx.Declaration.ClassName);
        Assert.Equal(methodName, _ctx.Declaration.MethodName);
        Assert.Equal(returnType, _ctx.Declaration.ReturnType);
        return this;
    }

    public MasterContextAsserts IsPublic()
    {
        Assert.Equal(AccessModifier.Public, _ctx.Declaration.AccessModifier);
        return this;
    }

    public MasterContextAsserts IsNotStatic()
    {
        Assert.False(_ctx.Declaration.IsStatic);
        return this;
    }

    public MasterContextAsserts IsPartialClass()
    {
        Assert.True(_ctx.Declaration.IsPartialClass);
        return this;
    }

    public MasterContextAsserts IsNotExtensionMethod()
    {
        Assert.False(_ctx.Declaration.IsExtensionMethod);
        return this;
    }

    public MasterContextAsserts HasParameters(params (string Type, string Name)[] expected)
    {
        var actual = _ctx.Declaration.Parameters;
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Type, actual[i].Type);
            Assert.Equal(expected[i].Name, actual[i].Name);
        }
        return this;
    }

    // -----------------------------------------------------------------------------------------
    // Allowed rules & disabled flag
    // -----------------------------------------------------------------------------------------

    public MasterContextAsserts AllowsAllRules()
    {
        Assert.True(_ctx.AllowedRules.AllowRule1);
        Assert.True(_ctx.AllowedRules.AllowRule2);
        Assert.True(_ctx.AllowedRules.AllowRule3);
        Assert.True(_ctx.AllowedRules.AllowRule4);
        Assert.True(_ctx.AllowedRules.AllowRule5);
        return this;
    }

    // -----------------------------------------------------------------------------------------
    // Apply flags
    // -----------------------------------------------------------------------------------------

    public MasterContextAsserts AppliesNoRules()
        => AppliesRules(rule1: false, rule2: false, rule3: false, rule4: false, rule5: false);

    public MasterContextAsserts AppliesRules(bool rule1, bool rule2, bool rule3, bool rule4, bool rule5)
    {
        Assert.Equal(rule1, _ctx.ApplyRule1);
        Assert.Equal(rule2, _ctx.ApplyRule2);
        Assert.Equal(rule3, _ctx.ApplyRule3);
        Assert.Equal(rule4, _ctx.ApplyRule4);
        Assert.Equal(rule5, _ctx.ApplyRule5);
        return this;
    }

    // -----------------------------------------------------------------------------------------
    // Rule contexts
    // -----------------------------------------------------------------------------------------

    public MasterContextAsserts HasNoRule1Contexts()
    {
        Assert.True(_ctx.Rule1Contexts is null or { Count: 0 });
        return this;
    }

    public MasterContextAsserts HasRule1Delegates(params string[] paramNames)
    {
        Assert.NotNull(_ctx.Rule1Contexts);
        Assert.Equal(paramNames, _ctx.Rule1Contexts.Select(c => c.Delegate.ParameterName).ToArray());
        return this;
    }

    public MasterContextAsserts HasNoRule2Contexts()
    {
        Assert.True(_ctx.Rule2Contexts is null or { Count: 0 });
        return this;
    }

    public MasterContextAsserts HasRule2Delegates(params string[] paramNames)
    {
        Assert.NotNull(_ctx.Rule2Contexts);
        Assert.Equal(paramNames, _ctx.Rule2Contexts.Select(c => c.Delegate.ParameterName).ToArray());
        return this;
    }

    public MasterContextAsserts HasNoRule3Contexts()
    {
        Assert.True(_ctx.Rule3Contexts is null or { Count: 0 });
        return this;
    }

    public MasterContextAsserts HasRule3Contexts(params (string Name, int K)[] expected)
    {
        Assert.NotNull(_ctx.Rule3Contexts);
        var actual = _ctx.Rule3Contexts
            .Select(c => (c.Delegate.ParameterName, c.TargetInputCount))
            .ToArray();
        Assert.Equal(expected.Select(e => (e.Name, e.K)).ToArray(), actual);
        return this;
    }

    public MasterContextAsserts HasNoRule4Context()
    {
        Assert.Null(_ctx.Rule4Context);
        return this;
    }

    public MasterContextAsserts HasNoRule5Context()
    {
        Assert.Null(_ctx.Rule5Context);
        return this;
    }

    public MasterContextAsserts HasRule5AttributedParameters(params string[] paramNames)
    {
        Assert.NotNull(_ctx.Rule5Context);
        Assert.Equal(paramNames, _ctx.Rule5Context.AttributedParameters.Select(d => d.ParameterName).ToArray());
        return this;
    }
}
