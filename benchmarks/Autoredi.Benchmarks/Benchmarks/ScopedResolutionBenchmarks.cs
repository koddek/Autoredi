using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Autoredi.Benchmarks.Autoredi;
using Autoredi.Benchmarks.Services;

namespace Autoredi.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing scoped service resolution performance.
/// </summary>
[MemoryDiagnoser]
public class ScopedResolutionBenchmarks
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
        manualServices.AddScoped<IServiceB, ManualServiceB>();
        _manualProvider = manualServices.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _autorediProvider?.Dispose();
        _manualProvider?.Dispose();
    }

    [Benchmark(Description = "Autoredi - Create Scope + Resolve")]
    public IServiceB AutorediScopedResolution()
    {
        using var scope = _autorediProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IServiceB>();
    }

    [Benchmark(Description = "Manual - Create Scope + Resolve", Baseline = true)]
    public IServiceB ManualScopedResolution()
    {
        using var scope = _manualProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IServiceB>();
    }
}
