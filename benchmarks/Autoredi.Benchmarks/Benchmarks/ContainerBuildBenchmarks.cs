using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Autoredi.Benchmarks.Autoredi;
using Autoredi.Benchmarks.Services;

namespace Autoredi.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing container build performance between Autoredi and manual registration.
/// </summary>
[MemoryDiagnoser]
public class ContainerBuildBenchmarks
{
    [Benchmark(Description = "Autoredi - AddAutorediServices")]
    public ServiceProvider AutorediRegistration()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        return services.BuildServiceProvider();
    }

    [Benchmark(Description = "Manual - Individual Add* calls", Baseline = true)]
    public ServiceProvider ManualRegistration()
    {
        var services = new ServiceCollection();

        // Singleton services
        services.AddSingleton<IServiceA, ManualServiceA>();
        services.AddSingleton<ManualConcreteService>();

        // Scoped services
        services.AddScoped<IServiceB, ManualServiceB>();

        // Transient services
        services.AddTransient<IServiceC, ManualServiceC>();

        // Keyed services
        services.AddKeyedSingleton<IKeyedService, ManualKeyedService1>("key1");
        services.AddKeyedSingleton<IKeyedService, ManualKeyedService2>("key2");
        services.AddKeyedSingleton<IKeyedService, ManualKeyedService3>("key3");

        return services.BuildServiceProvider();
    }
}
