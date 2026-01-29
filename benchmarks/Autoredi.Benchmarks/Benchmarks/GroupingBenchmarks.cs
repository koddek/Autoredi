using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Autoredi.Benchmarks.Autoredi;
using Autoredi.Benchmarks.Services;

namespace Autoredi.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing grouping and selective registration performance.
/// </summary>
[MemoryDiagnoser]
public class GroupingBenchmarks
{
    [Benchmark(Description = "Autoredi - Full Registration (All Groups)")]
    public void FullRegistration()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
    }

    [Benchmark(Description = "Autoredi - Selective Registration (GroupA Only)")]
    public void SelectiveRegistration()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesGroupA();
    }

    [Benchmark(Description = "Autoredi - Default Group Only")]
    public void DefaultGroupOnly()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesDefault();
    }
}
