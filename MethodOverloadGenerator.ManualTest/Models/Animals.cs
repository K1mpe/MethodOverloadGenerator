using System;
using System.Collections.Generic;
using System.Text;

namespace MethodOverloadGenerator.ManualTest.Models;

public class Cat : ICarnivore
{
    public Task EatPrey(IAnimal prey)
    {
        return Task.CompletedTask;
    }
}

public class Mouse : IAnimal
{
}

public class Bird : IAnimal
{
}