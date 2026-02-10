using Microsoft.Extensions.DependencyInjection;
using Samples.Basic.Autoredi;
using Samples.Basic.Services;

namespace Samples.Basic;

/// <summary>
/// Demonstrates basic Autoredi usage with concrete service registration.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Basic Demo ===");
        Console.WriteLine("Demonstrates concrete service registration without interfaces.\n");

        // 1. Create service collection
        var services = new ServiceCollection();

        // 2. Register all Autoredi services (generated extension method)
        services.AddAutorediServicesSamplesBasic();

        // 3. Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // 4. Resolve and use the service
        var config = serviceProvider.GetRequiredService<AppConfig>();

        Console.WriteLine("Resolved AppConfig service:");
        Console.WriteLine($"  Application Name: {config.ApplicationName}");
        Console.WriteLine($"  Version: {config.Version}");
        Console.WriteLine($"  Started At: {config.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");

        // Demonstrate singleton behavior
        Console.WriteLine("\nVerifying Singleton behavior (same instance):");
        var config2 = serviceProvider.GetRequiredService<AppConfig>();
        Console.WriteLine($"  Same instance? {ReferenceEquals(config, config2)}");

        Console.WriteLine("\n=== Demo Complete ===");
    }
}
