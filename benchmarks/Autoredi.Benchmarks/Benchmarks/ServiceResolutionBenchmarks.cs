using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Autoredi.Benchmarks.Autoredi;
using Autoredi.Benchmarks.Services;

namespace Autoredi.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing service resolution performance between Autoredi and manual registration.
/// </summary>
[MemoryDiagnoser]
public class ServiceResolutionBenchmarks
{
    private ServiceProvider _autorediProvider = null!;
    private ServiceProvider _manualProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Autoredi container
        var autorediServices = new ServiceCollection();
        autorediServices.AddAutorediServices();
        _autorediProvider = autorediServices.BuildServiceProvider();

        // Setup Manual container
        var manualServices = new ServiceCollection();
        manualServices.AddSingleton<IServiceA, ManualServiceA>();
        manualServices.AddSingleton<ManualConcreteService>();
        manualServices.AddScoped<IServiceB, ManualServiceB>();
        manualServices.AddTransient<IServiceC, ManualServiceC>();
        manualServices.AddKeyedSingleton<IKeyedService, ManualKeyedService1>("key1");
        manualServices.AddKeyedSingleton<IKeyedService, ManualKeyedService2>("key2");
        manualServices.AddKeyedSingleton<IKeyedService, ManualKeyedService3>("key3");
        _manualProvider = manualServices.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _autorediProvider?.Dispose();
        _manualProvider?.Dispose();
    }

    // Singleton resolution benchmarks
    [Benchmark(Description = "Autoredi - Resolve Singleton")]
    public IServiceA AutorediResolveSingleton()
    {
        return _autorediProvider.GetRequiredService<IServiceA>();
    }

    [Benchmark(Description = "Manual - Resolve Singleton", Baseline = true)]
    public IServiceA ManualResolveSingleton()
    {
        return _manualProvider.GetRequiredService<IServiceA>();
    }

    // Transient resolution benchmarks
    [Benchmark(Description = "Autoredi - Resolve Transient")]
    public IServiceC AutorediResolveTransient()
    {
        return _autorediProvider.GetRequiredService<IServiceC>();
    }

    [Benchmark(Description = "Manual - Resolve Transient")]
    public IServiceC ManualResolveTransient()
    {
        return _manualProvider.GetRequiredService<IServiceC>();
    }

    // Keyed service resolution benchmarks
    [Benchmark(Description = "Autoredi - Resolve Keyed Service")]
    public IKeyedService AutorediResolveKeyed()
    {
        return _autorediProvider.GetKeyedService<IKeyedService>("key1")!;
    }

    [Benchmark(Description = "Manual - Resolve Keyed Service")]
    public IKeyedService ManualResolveKeyed()
    {
        return _manualProvider.GetKeyedService<IKeyedService>("key1")!;
    }

    // Concrete service resolution benchmarks
    [Benchmark(Description = "Autoredi - Resolve Concrete")]
    public AutorediConcreteService AutorediResolveConcrete()
    {
        return _autorediProvider.GetRequiredService<AutorediConcreteService>();
    }

    [Benchmark(Description = "Manual - Resolve Concrete")]
    public ManualConcreteService ManualResolveConcrete()
    {
        return _manualProvider.GetRequiredService<ManualConcreteService>();
    }
}
