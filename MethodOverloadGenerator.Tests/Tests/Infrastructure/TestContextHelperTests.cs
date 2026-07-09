namespace MethodOverloadGenerator.Tests.Infrastructure;

public class TestContextHelperTests
{
    [Fact]
    public void MethodPlacement_SimpleActionDelegate()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                [MethodOverloadGeneratorAttribute]
                public void M(System.Action f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesNoRules()
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void ClassPlacement_SimpleActionDelegate()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            [MethodOverloadGeneratorAttribute]
            public partial class C
            {
                public void M(System.Action f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Class)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesNoRules()
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void ParameterPlacement_OnDelegateParam()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                public void M([MethodOverloadGeneratorAttribute] System.Action f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Parameter)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesNoRules()
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void ParameterPlacement_OnNonDelegateParam()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                public void M([MethodOverloadGeneratorAttribute] int count, System.Action f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Parameter)
            .HasNonDelegateAttributedParamName("count")
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("int", "count"), ("Action", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesNoRules()
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void NamespacedClass_ReturnsInt()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            namespace Acme.Services
            {
                public partial class MyService
                {
                    [MethodOverloadGeneratorAttribute]
                    public int Process(System.Action f) => 0;
                }
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: "Acme.Services", className: "MyService", methodName: "Process", returnType: "int")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesNoRules()
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void AsyncDelegate_AppliesRule1()
    {
        TestContextHelper.CreateMasterContext("""
            using System.Threading.Tasks;
            using MethodOverloadGenerator;
            public partial class C
            {
                [MethodOverloadGeneratorAttribute]
                public void M(System.Func<Task> f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Func<Task>", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesRules(rule1: true, rule2: false, rule3: false, rule4: false, rule5: false)
            .HasRule1Delegates("f")
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void ValueReturningDelegate_AppliesRule2()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                [MethodOverloadGeneratorAttribute]
                public void M(System.Func<int> f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Func<int>", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesRules(rule1: false, rule2: true, rule3: false, rule4: false, rule5: false)
            .HasNoRule1Contexts()
            .HasRule2Delegates("f")
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void MultiInputDelegate_AppliesRule3()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                [MethodOverloadGeneratorAttribute]
                public void M(System.Action<int, string> f) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action<int, string>", "f"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesRules(rule1: false, rule2: false, rule3: true, rule4: false, rule5: false)
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasRule3Contexts(("f", 1), ("f", 0))
            .HasNoRule4Context()
            .HasNoRule5Context();
    }

    [Fact]
    public void TwoDelegates_AppliesRule5()
    {
        TestContextHelper.CreateMasterContext("""
            using MethodOverloadGenerator;
            public partial class C
            {
                [MethodOverloadGeneratorAttribute]
                public void M(System.Action f, System.Action g) {}
            }
            """)
            .Assert()
            .HasAttributePlacement(AttributePlacement.Method)
            .HasNonDelegateAttributedParamName(null)
            .HasNoOutOrRefParams()
            .HasDeclaration(ns: null, className: "C", methodName: "M", returnType: "void")
            .IsPublic()
            .IsNotStatic()
            .IsPartialClass()
            .IsNotExtensionMethod()
            .HasParameters(("Action", "f"), ("Action", "g"))
            .AllowsAllRules()
            .HasNoApplicableRuleDisabled()
            .AppliesRules(rule1: false, rule2: false, rule3: false, rule4: false, rule5: true)
            .HasNoRule1Contexts()
            .HasNoRule2Contexts()
            .HasNoRule3Contexts()
            .HasNoRule4Context()
            .HasRule5AttributedParameters("f", "g");
    }

    [Fact]
    public void MissingAttribute_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestContextHelper.CreateMasterContext("""
                public partial class C
                {
                    public void M(System.Action f) {}
                }
                """));
    }
}
