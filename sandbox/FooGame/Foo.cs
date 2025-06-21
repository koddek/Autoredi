using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace FooGame;

public interface IFoo
{
    void Activate();
}

[Autoredi(ServiceLifetime.Singleton, typeof(IFoo))]
public class Foo : IFoo
{
    public void Activate()
    {
        Console.WriteLine("Foo Activated.");
    }
}

public interface IRepo
{
    void Get(int id);
}

[Autoredi(interfaceType: typeof(IRepo), serviceKey: Keys.Human)]
public class HumanArmorRepo : IRepo
{
    public void Get(int id)
    {
        Console.WriteLine($"[HUMAN ARMORY] NO:{id}");
    }
}

[Autoredi(interfaceType: typeof(IRepo), serviceKey: Keys.Alien)]
public class AlienWeaponRepo : IRepo
{
    public void Get(int id)
    {
        Console.WriteLine($"Alien Weapon  NO:{id}");
    }
}

public static class Keys
{
    public const string Email = "email";
    public const string SMS = "sms";
    public const string Human = "human";
    public const string Alien = "alien";
}
