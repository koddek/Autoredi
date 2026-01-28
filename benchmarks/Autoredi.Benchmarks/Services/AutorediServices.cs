using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Benchmarks.Services;

// Interfaces
public interface IServiceA
{
    void DoWork();
}

public interface IServiceB
{
    void DoWork();
}

public interface IServiceC
{
    void DoWork();
}

public interface IKeyedService
{
    string Name { get; }
    void DoWork();
}

// Autoredi-registered services
[Autoredi(ServiceLifetime.Singleton, typeof(IServiceA))]
public class AutorediServiceA : IServiceA
{
    public void DoWork() { }
}

[Autoredi(ServiceLifetime.Scoped, typeof(IServiceB))]
public class AutorediServiceB : IServiceB
{
    public void DoWork() { }
}

[Autoredi(ServiceLifetime.Transient, typeof(IServiceC))]
public class AutorediServiceC : IServiceC
{
    public void DoWork() { }
}

// Keyed services
[Autoredi(ServiceLifetime.Singleton, typeof(IKeyedService), "key1")]
public class AutorediKeyedService1 : IKeyedService
{
    public string Name => "Key1";
    public void DoWork() { }
}

[Autoredi(ServiceLifetime.Singleton, typeof(IKeyedService), "key2")]
public class AutorediKeyedService2 : IKeyedService
{
    public string Name => "Key2";
    public void DoWork() { }
}

[Autoredi(ServiceLifetime.Singleton, typeof(IKeyedService), "key3")]
public class AutorediKeyedService3 : IKeyedService
{
    public string Name => "Key3";
    public void DoWork() { }
}

// Concrete singleton
[Autoredi(ServiceLifetime.Singleton)]
public class AutorediConcreteService
{
    public Guid Id { get; } = Guid.NewGuid();
}
