using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.KeyedServices.Autoredi;
using Samples.KeyedServices.Constants;

namespace Samples.KeyedServices;

/// <summary>
/// Demonstrates keyed service registration with multiple implementations of the same interface.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Keyed Services Demo ===");
        Console.WriteLine("Demonstrates multiple implementations of the same interface with keys.\n");

        // 1. Create service collection
        var services = new ServiceCollection();

        // 2. Register all Autoredi services (generated extension method)
        services.AddAutorediServices();

        // 3. Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // 4. Resolve services by key
        Console.WriteLine("Resolving notification services by key:\n");

        // Email
        var emailService = serviceProvider.GetKeyedService<INotificationService>(ServiceKeys.Email);
        if (emailService != null)
        {
            Console.WriteLine($"Channel: {emailService.Channel}");
            emailService.Send("Welcome to our service!");
            Console.WriteLine();
        }

        // SMS
        var smsService = serviceProvider.GetKeyedService<INotificationService>(ServiceKeys.SMS);
        if (smsService != null)
        {
            Console.WriteLine($"Channel: {smsService.Channel}");
            smsService.Send("Your verification code is 123456");
            Console.WriteLine();
        }

        // Push
        var pushService = serviceProvider.GetKeyedService<INotificationService>(ServiceKeys.Push);
        if (pushService != null)
        {
            Console.WriteLine($"Channel: {pushService.Channel}");
            pushService.Send("You have a new message!");
            Console.WriteLine();
        }

        // 5. Demonstrate getting all services
        Console.WriteLine("All registered notification services:");
        var allServices = serviceProvider.GetServices<INotificationService>();
        foreach (var service in allServices)
        {
            Console.WriteLine($"  - {service.Channel}");
        }

        // 6. Demonstrate keyed service factory pattern
        Console.WriteLine("\nUsing factory pattern to resolve services dynamically:");
        services.AddSingleton<Func<string, INotificationService>>(sp => key =>
            sp.GetKeyedService<INotificationService>(key)!);

        var factoryServiceProvider = services.BuildServiceProvider();
        var factory = factoryServiceProvider.GetRequiredService<Func<string, INotificationService>>();

        var dynamicService = factory(ServiceKeys.Email);
        dynamicService.Send("Factory-created email notification");

        Console.WriteLine("\n=== Demo Complete ===");
    }
}
