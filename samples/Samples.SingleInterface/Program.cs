using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.SingleInterface.Autoredi;

namespace Samples.SingleInterface;

/// <summary>
/// Demonstrates single interface implementation registration with Autoredi.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Single Interface Demo ===");
        Console.WriteLine("Demonstrates registering a single interface implementation.\n");

        // 1. Create service collection
        var services = new ServiceCollection();

        // 2. Register all Autoredi services (generated extension method)
        services.AddAutorediServices();

        // 3. Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // 4. Resolve by interface
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Console.WriteLine("Resolved ILogger service:");
        logger.Log("Application started successfully!");
        logger.Log("This is a transient service - each resolution creates a new instance.");

        // Demonstrate transient behavior
        Console.WriteLine("\nVerifying Transient behavior (different instances):");
        var logger2 = serviceProvider.GetRequiredService<ILogger>();
        logger2.Log("This is a different instance");
        Console.WriteLine($"  Same instance? {ReferenceEquals(logger, logger2)}");

        Console.WriteLine("\n=== Demo Complete ===");
    }
}
