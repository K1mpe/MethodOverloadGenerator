namespace MethodOverloadGenerator.Tests.Tests;

/// <summary>
/// Tests that the generated partial class file has the correct structure to merge cleanly
/// with the original class — correct namespace, access modifier, class-level modifiers,
/// generic parameters, nesting, and file-level hygiene.
///
/// None of these are stated explicitly in the spec but must hold for the generated code to
/// compile and behave correctly.
/// </summary>
public class GeneratedCodeStructureTests
{
    // -----------------------------------------------------------------------------------------
    // Namespace
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedPartialClass_HasSameNamespace_AsOriginalClass()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            namespace Animal.Services
            {
                public partial class AnimalShelter
                {
                    [MethodOverloadGeneratorAttribute]
                    public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
                }
            }
            """);

        Assert.Contains("namespace Animal.Services", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedPartialClass_FileScopedNamespace_PreservedInOutput()
    {
        // namespace Foo; (file-scoped) must be emitted the same way, or as a block namespace
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            namespace Animal.Services;

            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("namespace Animal.Services", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedPartialClass_ClassInGlobalNamespace_GeneratedWithNoNamespaceDeclaration()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.DoesNotContain("namespace ", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedPartialClass_DeepNestedNamespace_FullNamespacePathPreserved()
    {
        // e.g. namespace A.B.C — all segments must be present
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            namespace Animal.Services.Shelters;

            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("namespace Animal.Services.Shelters", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // `partial` keyword on the generated class declaration
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedClassDeclaration_AlwaysIncludesPartialKeyword()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("partial class AnimalShelter", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // Access modifier on the class declaration in the generated file
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedClassDeclaration_PublicOriginal_DeclaredPublic()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("public partial class AnimalShelter", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedClassDeclaration_InternalOriginal_DeclaredInternal()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            internal partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("internal partial class AnimalShelter", result.SingleGeneratedSource);
    }

    [Fact(Skip = "Requires nested-class support (a top-level class can't be private) — DeclarationContext has no notion of an outer containing type at all yet.")]
    public void GeneratedClassDeclaration_PrivateOriginal_DeclaredPrivate()
    {
        // private partial classes (nested) must retain private
    }

    [Fact(Skip = "Requires nested-class support (a top-level class can't be 'protected internal' either) — same gap as the private case above.")]
    public void GeneratedClassDeclaration_ProtectedInternalOriginal_DeclaredProtectedInternal()
    {
    }

    // -----------------------------------------------------------------------------------------
    // `static` modifier on the class
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedClassDeclaration_StaticOriginal_DeclaredStatic()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public static partial class ShelterUtilities
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<int> Compute(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("static partial class ShelterUtilities", result.SingleGeneratedSource);
    }

    [Fact]
    public void GeneratedClassDeclaration_NonStaticOriginal_NotDeclaredStatic()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.DoesNotContain("static partial class", result.SingleGeneratedSource);
    }

    // Note: `sealed` and `abstract` are deliberately NOT reproduced on the generated class shell.
    // Unlike generic type parameters or the class/record/struct kind, C# unions these modifiers
    // across partial declarations — a class is sealed/abstract if *any* part says so, so the
    // original declaration alone is enough; omitting them on the generated shell causes no issue.

    // -----------------------------------------------------------------------------------------
    // `record` and `record struct`
    // -----------------------------------------------------------------------------------------

    [Fact(Skip = "Not supported: the generator always emits 'partial class', regardless of the original type kind — for a record this would be CS0261 (partial declarations must agree on type kind), since record-ness isn't tracked anywhere in the pipeline.")]
    public void GeneratedClassDeclaration_RecordOriginal_DeclaredRecord()
    {
    }

    [Fact(Skip = "Not supported — same gap as GeneratedClassDeclaration_RecordOriginal_DeclaredRecord, for 'record struct'.")]
    public void GeneratedClassDeclaration_RecordStructOriginal_DeclaredRecordStruct()
    {
    }

    // -----------------------------------------------------------------------------------------
    // Generic type parameters on the class
    // -----------------------------------------------------------------------------------------

    [Fact(Skip = "Not supported: DeclarationContext.ClassName is just the bare name (e.g. \"Foo\", never \"Foo<T>\") — a generic class's generated shell would fail to compile with CS0305 (using the generic type requires a type argument).")]
    public void GeneratedClassDeclaration_GenericClass_TypeParametersPresent()
    {
        // class Foo<T> — generated partial must be Foo<T>
    }

    [Fact(Skip = "Not supported — same gap as GeneratedClassDeclaration_GenericClass_TypeParametersPresent, for multiple type parameters.")]
    public void GeneratedClassDeclaration_GenericClass_MultipleTypeParametersPreserved()
    {
    }

    [Fact(Skip = "Not supported — same gap as GeneratedClassDeclaration_GenericClass_TypeParametersPresent; class-level type constraints aren't tracked either (only the method's own generic constraints are, via DeclarationContext.TypeParameterConstraintClauses).")]
    public void GeneratedClassDeclaration_GenericClass_TypeConstraintsPreserved()
    {
        // where T : class, new() must appear on the generated partial declaration
    }

    // -----------------------------------------------------------------------------------------
    // Nested classes
    // -----------------------------------------------------------------------------------------

    [Fact(Skip = "Not supported: DeclarationContext has no notion of an outer containing type, so a nested class's generated shell is emitted at the top level instead of wrapped in its outer class(es).")]
    public void GeneratedPartialClass_NestedInOuterClass_OuterClassShellIncludedInGeneratedFile()
    {
        // The outer class must be re-declared (as partial) to wrap the inner partial
    }

    [Fact(Skip = "Not supported — same gap as GeneratedPartialClass_NestedInOuterClass_OuterClassShellIncludedInGeneratedFile.")]
    public void GeneratedPartialClass_NestedInOuterClass_OuterShell_DoesNotRepeatOuterMembers()
    {
        // The outer partial shell should be empty except for the nested class
    }

    [Fact(Skip = "Not supported — same gap as GeneratedPartialClass_NestedInOuterClass_OuterClassShellIncludedInGeneratedFile, for multiple levels of nesting.")]
    public void GeneratedPartialClass_DeeplyNested_AllOuterShellsPresent()
    {
    }

    // -----------------------------------------------------------------------------------------
    // Using directives / imports in the generated file
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedFile_ContainsUsingDirectives_RequiredForGeneratedCode()
    {
        // At minimum: System, System.Threading.Tasks for Task/ValueTask usage
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Contains("using System;", result.SingleGeneratedSource);
        Assert.Contains("using System.Threading.Tasks;", result.SingleGeneratedSource);
    }

    // Note: the generated file may contain redundant or unused using directives. PartialClassEmitter
    // deliberately copies every using from the original file verbatim (plus a small fixed baseline)
    // rather than computing the minimal set actually referenced — an intentional simplicity-over-
    // precision tradeoff (see PartialClassEmitter.BuildUsings). Extra usings are legal C# and cause
    // no compile error, so this isn't a correctness issue worth guarding with a test.

    // -----------------------------------------------------------------------------------------
    // [MethodOverloadGeneratorAttribute] attribute must NOT appear on generated overloads
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedOverloads_DoNotCarry_MethodOverloadGeneratorAttribute()
    {
        // Carrying the attribute would cause infinite recursion / re-generation
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.DoesNotContain("[MethodOverloadGenerator", result.SingleGeneratedSource);
    }

    // -----------------------------------------------------------------------------------------
    // The generated file is valid, parseable C#
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void GeneratedFile_IsValidCSharp_WithNoDiagnostics()
    {
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<int> FetchCapacity(Func<Task<int>> fetch) => fetch();
            }
            """);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GeneratedFile_WhenAddedToOriginalCompilation_ProducesNoNewErrors()
    {
        // A richer scenario — multiple rules firing simultaneously (sync, fixed-value, and their
        // combination) — compiled together with the original source via the same driver call
        // TestHelper uses, so any signature clash or malformed overload would surface here.
        var result = TestHelper.RunGenerator("""
            using MethodOverloadGenerator;
            public partial class AnimalShelter
            {
                [MethodOverloadGeneratorAttribute]
                public Task<bool> Admit(Func<Task<int>> fetchCapacity, Func<Task<string>> fetchName) => Task.FromResult(true);
            }
            """);

        Assert.Empty(result.Errors);
    }
}
