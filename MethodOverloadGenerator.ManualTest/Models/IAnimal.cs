namespace MethodOverloadGenerator.ManualTest.Models;

public interface IAnimal
{
}

public interface ICarnivore : IAnimal
{
    public Task EatPrey(IAnimal prey);   
}

public interface IHerbivore : IAnimal
{

}