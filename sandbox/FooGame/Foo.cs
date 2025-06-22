using Autoredi.Attributes;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FooGame;

[Autoredi(ServiceLifetime.Transient)]
public class MyService2
{
    private readonly IFoo _foo;

    public MyService2(IFoo foo)
    {
        _foo = foo;
    }

    public void Activate()
    {
        _foo.Activate();
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
