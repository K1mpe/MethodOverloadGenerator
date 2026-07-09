using MethodOverloadGenerator.Generation.Rules;

namespace MethodOverloadGenerator.Tests.Generation.Rules;

/// <summary>
/// <c>Task&lt;T&gt;</c> is not covariant, so a receiver overload hard-coded as
/// <c>this Task&lt;ICarnivore&gt; animal</c> would reject a caller's <c>Task&lt;Dog&gt;</c> even
/// though <c>Dog</c> implements <c>ICarnivore</c> — exactly the "chain off an async result" case
/// the generator exists for. Whenever the <c>this</c> parameter's type could have subtypes (an
/// interface or a non-sealed class), the emitter must introduce a fresh type parameter
/// constrained to that type instead, e.g. <c>Feed&lt;TCarnivore&gt;(this Task&lt;TCarnivore&gt; animal)
/// where TCarnivore : ICarnivore</c>. When the type cannot have subtypes (sealed class, struct,
/// built-in value type) or is already a type parameter of the original method, no such
/// substitution is needed or possible.
/// </summary>
public class Rule4EmitterTests
{
    private readonly Rule4Emitter _sut = new();

    private IReadOnlyList<string> Emit(MasterContext ctx)
    {
        var result = _sut.Emit(ctx.Rule4Context!, ctx.Declaration);
        Assert.Equal(2, result.Count);
        return result;
    }

    // -----------------------------------------------------------------------------------------
    // Async extension method — receiver is an interface, so a constrained type parameter
    // is synthesized ("TCarnivore" from "ICarnivore")
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncExtensionMethod_InterfaceReceiver_TaskOfT_NoOtherParams()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();",
            "public static async Task<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    // -----------------------------------------------------------------------------------------
    // Async extension method — returns plain Task, with an extra parameter forwarded
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncExtensionMethod_InterfaceReceiver_PlainTask_ForwardsOtherParam()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task Feed<TCarnivore>(this Task<TCarnivore> animal, Func<Task<IPrey>> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(getPrey);",
            "public static async Task Feed<TCarnivore>(this ValueTask<TCarnivore> animal, Func<Task<IPrey>> getPrey) where TCarnivore : ICarnivore => await (await animal).Feed(getPrey);");
    }

    // -----------------------------------------------------------------------------------------
    // Async extension method — returns ValueTask<TOut> (return type kept as-is, not converted to Task)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AsyncExtensionMethod_InterfaceReceiver_ValueTaskOfT_ReturnTypePreserved()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static ValueTask<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async ValueTask<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();",
            "public static async ValueTask<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    // -----------------------------------------------------------------------------------------
    // Non-async extension method — receiver is a non-sealed class, so a constrained type
    // parameter is synthesized ("TCat" from "Cat"); overload becomes async
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncExtensionMethod_NonSealedClassReceiver_ValueReturn_WrapsReturnTypeInTask()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public class Cat {}
            public class Mouse {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<Mouse> GetFood<TCat>(this Task<TCat> cat) where TCat : Cat => (await cat).GetFood();",
            "public static async Task<Mouse> GetFood<TCat>(this ValueTask<TCat> cat) where TCat : Cat => (await cat).GetFood();");
    }

    // -----------------------------------------------------------------------------------------
    // Non-async extension method — returns void, overload becomes async Task
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SyncExtensionMethod_NonSealedClassReceiver_VoidReturn_OverloadReturnsTask()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static void Sedate(this Cat cat) => throw new NotImplementedException();
            }
            public class Cat {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task Sedate<TCat>(this Task<TCat> cat) where TCat : Cat => (await cat).Sedate();",
            "public static async Task Sedate<TCat>(this ValueTask<TCat> cat) where TCat : Cat => (await cat).Sedate();");
    }

    // -----------------------------------------------------------------------------------------
    // Access modifier
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AccessModifier_IsPreserved()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                internal static Task<IPrey> Feed(this ICarnivore animal) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "internal static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();",
            "internal static async Task<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> animal) where TCarnivore : ICarnivore => await (await animal).Feed();");
    }

    // -----------------------------------------------------------------------------------------
    // This-parameter name — preserved from the original, not renamed
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void ThisParameterName_IsPreservedFromOriginal()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<IPrey> Feed(this ICarnivore predator) => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<IPrey> Feed<TCarnivore>(this Task<TCarnivore> predator) where TCarnivore : ICarnivore => await (await predator).Feed();",
            "public static async Task<IPrey> Feed<TCarnivore>(this ValueTask<TCarnivore> predator) where TCarnivore : ICarnivore => await (await predator).Feed();");
    }

    // -----------------------------------------------------------------------------------------
    // Multiple other parameters — forwarded in declaration order, no `this` prefix
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void MultipleOtherParameters_ForwardedInOrder()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task Migrate(this IBird bird, string destination, int speed) => throw new NotImplementedException();
            }
            public interface IBird {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task Migrate<TBird>(this Task<TBird> bird, string destination, int speed) where TBird : IBird => await (await bird).Migrate(destination, speed);",
            "public static async Task Migrate<TBird>(this ValueTask<TBird> bird, string destination, int speed) where TBird : IBird => await (await bird).Migrate(destination, speed);");
    }

    // -----------------------------------------------------------------------------------------
    // Sealed class / built-in receiver — no subtype is possible, so no type parameter is
    // synthesized and the concrete type is used directly (this is safe: Task<string> is the
    // only possible caller-side task since string cannot be subclassed)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SealedClassReceiver_NoGenericSynthesized_UsesConcreteTypeDirectly()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Mouse GetFood(this Cat cat) => throw new NotImplementedException();
            }
            public sealed class Cat {}
            public class Mouse {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<Mouse> GetFood(this Task<Cat> cat) => (await cat).GetFood();",
            "public static async Task<Mouse> GetFood(this ValueTask<Cat> cat) => (await cat).GetFood();");
    }

    [Fact]
    public void BuiltInSealedTypeReceiver_NoGenericSynthesized_UsesConcreteTypeDirectly()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class StringExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static int Shout(this string s) => throw new NotImplementedException();
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<int> Shout(this Task<string> s) => (await s).Shout();",
            "public static async Task<int> Shout(this ValueTask<string> s) => (await s).Shout();");
    }

    [Fact]
    public void StructReceiver_NoGenericSynthesized_UsesConcreteTypeDirectly()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class PointExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static double Magnitude(this Point p) => throw new NotImplementedException();
            }
            public struct Point {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<double> Magnitude(this Task<Point> p) => (await p).Magnitude();",
            "public static async Task<double> Magnitude(this ValueTask<Point> p) => (await p).Magnitude();");
    }

    // -----------------------------------------------------------------------------------------
    // Receiver is already a type parameter of the original generic method — reused verbatim,
    // no new type parameter or constraint is introduced
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AlreadyGenericReceiver_ReusesOriginalTypeParameter_NoConstraint()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class IdentityExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static TIn Identity<TIn>(this TIn input) => input;
            }
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<TIn> Identity<TIn>(this Task<TIn> input) => (await input).Identity();",
            "public static async Task<TIn> Identity<TIn>(this ValueTask<TIn> input) => (await input).Identity();");
    }

    // -----------------------------------------------------------------------------------------
    // Receiver is already a type parameter of the original generic method, AND that type
    // parameter has its own constraint — the constraint must still be reproduced even though
    // no new type parameter or Rule-4-specific constraint is introduced
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void AlreadyGenericReceiver_WithConstraint_PreservesOriginalConstraint()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task<T> Eat<T>(this T animal, Func<Task<IPrey>> fetchPrey) where T : ICarnivore => throw new NotImplementedException();
            }
            public interface ICarnivore {}
            public interface IPrey {}
            """);

        var result = Emit(ctx);

        TestContextHelper.NormalisedContains(result,
            "public static async Task<T> Eat<T>(this Task<T> animal, Func<Task<IPrey>> fetchPrey) where T : ICarnivore => await (await animal).Eat(fetchPrey);",
            "public static async Task<T> Eat<T>(this ValueTask<T> animal, Func<Task<IPrey>> fetchPrey) where T : ICarnivore => await (await animal).Eat(fetchPrey);");
    }

    // -----------------------------------------------------------------------------------------
    // IntelliSense/overload-resolution priority — no delegate parameter's arity or async-ness
    // changes here, only the receiver's own type, so both wrappers rank at -1: immediately below
    // the untouched original, and — when the method has async delegate parameters — above Rule 1
    // (which always turns them synchronous and so incurs an extra "lost async" point).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Priority_IsMinusOne_ForBothWrappers()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task Migrate(this IBird bird, string destination, int speed) => throw new NotImplementedException();
            }
            public interface IBird {}
            """);

        var result = Emit(ctx);

        Assert.All(result, s => Assert.Contains("[OverloadResolutionPriority(-1)]", s));
    }

    [Fact]
    public void Priority_RanksAboveRule1_WhenMethodHasAsyncDelegateParameters()
    {
        var ctx = TestContextHelper.CreateMasterContext("""
            public static partial class AnimalExtensions
            {
                [MethodOverloadGeneratorAttribute]
                public static Task Migrate(this IBird bird, Func<Task<int>> fetchSpeed) => throw new NotImplementedException();
            }
            public interface IBird {}
            """);

        var rule4Result = Emit(ctx);
        var rule1Result = new Rule1Emitter().Emit(ctx.Rule1Contexts!, ctx.Declaration);

        // Rule 4 keeps fetchSpeed fully async → -1; Rule 1 turns it sync → -2, so Rule 4 ranks higher.
        Assert.All(rule4Result, s => Assert.Contains("[OverloadResolutionPriority(-1)]", s));
        Assert.All(rule1Result, s => Assert.Contains("[OverloadResolutionPriority(-2)]", s));
    }
}
