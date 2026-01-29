using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Autoredi.Benchmarks.Autoredi;
using Autoredi.Benchmarks.Services.Generated;

namespace Autoredi.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class ComprehensiveRegistrationBenchmarks
{
    // ========================================================================
    // 1. Full Registration (All Services)
    // ========================================================================

    [Benchmark(Description = "Autoredi - All Groups (40 Services)", Baseline = false)]
    [BenchmarkCategory("Full_Registration")]
    public void Autoredi_All()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
    }

    [Benchmark(Description = "Manual - All Groups (40 Services)", Baseline = true)]
    [BenchmarkCategory("Full_Registration")]
    public void Manual_All()
    {
        var services = new ServiceCollection();
        
        // Singletons
        services.AddSingleton<Singleton_0>();
        services.AddSingleton<Singleton_1>();
        services.AddSingleton<Singleton_2>();
        services.AddSingleton<Singleton_3>();
        services.AddSingleton<Singleton_4>();
        services.AddSingleton<Singleton_5>();
        services.AddSingleton<Singleton_6>();
        services.AddSingleton<Singleton_7>();
        services.AddSingleton<Singleton_8>();
        services.AddSingleton<Singleton_9>();

        // Scopeds
        services.AddScoped<Scoped_0>();
        services.AddScoped<Scoped_1>();
        services.AddScoped<Scoped_2>();
        services.AddScoped<Scoped_3>();
        services.AddScoped<Scoped_4>();
        services.AddScoped<Scoped_5>();
        services.AddScoped<Scoped_6>();
        services.AddScoped<Scoped_7>();
        services.AddScoped<Scoped_8>();
        services.AddScoped<Scoped_9>();

        // Transients
        services.AddTransient<Transient_0>();
        services.AddTransient<Transient_1>();
        services.AddTransient<Transient_2>();
        services.AddTransient<Transient_3>();
        services.AddTransient<Transient_4>();
        services.AddTransient<Transient_5>();
        services.AddTransient<Transient_6>();
        services.AddTransient<Transient_7>();
        services.AddTransient<Transient_8>();
        services.AddTransient<Transient_9>();

        // Priority
        services.AddSingleton<PriorityHigh_0>();
        services.AddScoped<PriorityHigh_1>();
        services.AddTransient<PriorityHigh_2>();
        services.AddSingleton<PriorityMid_0>();
        services.AddScoped<PriorityMid_1>();
        services.AddTransient<PriorityMid_2>();
        services.AddSingleton<PriorityLow_0>();
        services.AddScoped<PriorityLow_1>();
        services.AddTransient<PriorityLow_2>();
        services.AddSingleton<PriorityMin_0>();
    }

    // ========================================================================
    // 2. Lifetime Specific Groups
    // ========================================================================

    [Benchmark(Description = "Autoredi - Singletons (10)", Baseline = false)]
    [BenchmarkCategory("Lifetime_Singleton")]
    public void Autoredi_Singletons()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesSingletons();
    }

    [Benchmark(Description = "Manual - Singletons (10)", Baseline = true)]
    [BenchmarkCategory("Lifetime_Singleton")]
    public void Manual_Singletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Singleton_0>();
        services.AddSingleton<Singleton_1>();
        services.AddSingleton<Singleton_2>();
        services.AddSingleton<Singleton_3>();
        services.AddSingleton<Singleton_4>();
        services.AddSingleton<Singleton_5>();
        services.AddSingleton<Singleton_6>();
        services.AddSingleton<Singleton_7>();
        services.AddSingleton<Singleton_8>();
        services.AddSingleton<Singleton_9>();
    }

    [Benchmark(Description = "Autoredi - Transients (10)", Baseline = false)]
    [BenchmarkCategory("Lifetime_Transient")]
    public void Autoredi_Transients()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesTransients();
    }

    [Benchmark(Description = "Manual - Transients (10)", Baseline = true)]
    [BenchmarkCategory("Lifetime_Transient")]
    public void Manual_Transients()
    {
        var services = new ServiceCollection();
        services.AddTransient<Transient_0>();
        services.AddTransient<Transient_1>();
        services.AddTransient<Transient_2>();
        services.AddTransient<Transient_3>();
        services.AddTransient<Transient_4>();
        services.AddTransient<Transient_5>();
        services.AddTransient<Transient_6>();
        services.AddTransient<Transient_7>();
        services.AddTransient<Transient_8>();
        services.AddTransient<Transient_9>();
    }

    // ========================================================================
    // 3. Priority & Mixing
    // ========================================================================

    [Benchmark(Description = "Autoredi - Prioritized Group (10 Mixed)", Baseline = false)]
    [BenchmarkCategory("Priority_Mixed")]
    public void Autoredi_Prioritized()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesPrioritized();
    }

    [Benchmark(Description = "Manual - Prioritized Group (10 Mixed)", Baseline = true)]
    [BenchmarkCategory("Priority_Mixed")]
    public void Manual_Prioritized()
    {
        var services = new ServiceCollection();
        // Manually registering in the correct priority order to match Autoredi's output
        // High (100)
        services.AddSingleton<PriorityHigh_0>();
        services.AddScoped<PriorityHigh_1>();
        services.AddTransient<PriorityHigh_2>();
        // Mid (50)
        services.AddSingleton<PriorityMid_0>();
        services.AddScoped<PriorityMid_1>();
        services.AddTransient<PriorityMid_2>();
        // Low (0)
        services.AddSingleton<PriorityLow_0>();
        services.AddScoped<PriorityLow_1>();
        services.AddTransient<PriorityLow_2>();
        // Min (0)
        services.AddSingleton<PriorityMin_0>();
    }
}
