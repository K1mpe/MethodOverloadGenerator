using MethodOverloadGenerator;
using MethodOverloadGenerator.ManualTest.Models;

namespace ManualTest;


[MethodOverloadGenerator]
public static partial class Methods
{
    internal static async Task<T> Eat<T>(this T animal, Func<CancellationToken, Task<IAnimal>> fetchPrey) where T : ICarnivore
    {
        var prey = await fetchPrey(CancellationToken.None);
        await animal.EatPrey(prey);
        return animal;
    }

    internal static async Task<T> Eat<T, TAnimal>(this T animal, Func<CancellationToken, Task<TAnimal>> fetchPrey) where T : ICarnivore
        where TAnimal : IAnimal
        => await Eat(animal, async (token) => await fetchPrey(token));

    internal static Cat LetCatInside(this Cat cat)
    {
        return cat;
    }

    internal static void SellAnimal(this IAnimal animal)
    {
    }

    internal static async Task<List<IAnimal>> Generic(int amount, int legs, bool canFly, bool canSwim, GenerateGenericAnimal fetchCarnivore, Func<CancellationToken, Task<IAnimal>> fetchPrey)
    {
        var carnivores = new List<IAnimal>();
        for (int i = 0; i < amount; i++)
        {
            var carnivore = await fetchCarnivore(legs, canFly, canSwim, "male", CancellationToken.None);
            carnivores.Add(carnivore);
        }
        return carnivores;
    }

    public delegate Task<IAnimal> GenerateGenericAnimal(int legs, bool canFly, bool canSwim, string maleOrFemale, CancellationToken token);

    [MethodOverloadGenerator]
    public static void Test(int abc)
    {

    }

}

public partial class LoopTimer
{
    [MethodOverloadGenerator]
    public LoopTimer(Func<Task<TimeSpan?>> delay, Func<CancellationToken, Task> Action)
    {
        
    }

    public void Reset() { }
}

public class Test
{

    public async Task<Cat> BuyNewCat()
    {
        await Task.Delay(1000);
        var dog = new Cat();
        return dog;
        
    }
    public async Task Run()
    {
        await BuyNewCat()
            .Eat(FetchPrey)
            .SellAnimal();
            
        await Methods.Generic(5, 4, false, true, async(legs, canFly) =>
        {
            await Task.Yield();
            if (canFly)
                return new Bird();
            return new Cat();
        }, () => new Mouse());

        var timespan = TimeSpan.FromMinutes(5);

        var timer = new LoopTimer(() => timespan, () =>
        {
            //Do something
        });
    }

    private Mouse FetchPrey(CancellationToken token)
    {
        return new Mouse();
    }
}
