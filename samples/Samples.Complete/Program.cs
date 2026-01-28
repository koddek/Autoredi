using Microsoft.Extensions.DependencyInjection;
using Samples.Complete.Autoredi;
using Samples.Complete.Controllers;
using Samples.Complete.Services;
using Samples.Common.Interfaces;
using Samples.Common.Models;

namespace Samples.Complete;

/// <summary>
/// Complete demonstration of all Autoredi features in a realistic application scenario.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Complete Demo ===");
        Console.WriteLine("Demonstrates all Autoredi features in a realistic application.\n");

        // 1. Create service collection
        var services = new ServiceCollection();

        // 2. Register all Autoredi services (generated extension method)
        services.AddAutorediServices();

        // 3. Register factory for dynamic keyed service resolution
        services.AddSingleton<Func<string, INotificationService>>(sp => key =>
            sp.GetKeyedService<INotificationService>(key)!);

        // 4. Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // 5. Demonstrate concrete service (Singleton)
        Console.WriteLine("1. Concrete Singleton Service:");
        var settings = serviceProvider.GetRequiredService<AppSettings>();
        Console.WriteLine($"   Application: {settings.ApplicationName}");
        Console.WriteLine($"   Started: {settings.StartedAt:yyyy-MM-dd HH:mm:ss} UTC\n");

        // 6. Demonstrate scoped services with scope
        Console.WriteLine("2. Scoped Services:");
        using (var scope = serviceProvider.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            userService.DisplayAppInfo();

            Console.WriteLine("\n   Users in system:");
            foreach (var user in userService.GetAllUsers())
            {
                Console.WriteLine($"     - {user.Name} ({user.Email})");
            }
        }
        Console.WriteLine();

        // 7. Demonstrate keyed services
        Console.WriteLine("3. Keyed Services:");
        var emailService = serviceProvider.GetKeyedService<INotificationService>(NotificationKeys.Email);
        emailService?.Send("Welcome to the complete demo!");

        var smsService = serviceProvider.GetKeyedService<INotificationService>(NotificationKeys.SMS);
        smsService?.Send("Your demo code is 12345");

        var slackService = serviceProvider.GetKeyedService<INotificationService>(NotificationKeys.Slack);
        slackService?.Send("New user registered!");
        Console.WriteLine();

        // 8. Demonstrate controller with keyed service injection
        Console.WriteLine("4. Controller with Keyed Service Injection:");
        var notificationController = serviceProvider.GetRequiredService<NotificationController>();
        notificationController.NotifyUser("123", "Your order has been shipped!");
        Console.WriteLine();

        // 9. Demonstrate factory pattern
        Console.WriteLine("5. Factory Pattern for Dynamic Resolution:");
        var factory = serviceProvider.GetRequiredService<Func<string, INotificationService>>();
        var dynamicService = factory(NotificationKeys.Slack);
        dynamicService.Send("Factory-created notification");
        Console.WriteLine();

        // 10. Demonstrate singleton behavior
        Console.WriteLine("6. Singleton Behavior Verification:");
        var settings2 = serviceProvider.GetRequiredService<AppSettings>();
        Console.WriteLine($"   Same AppSettings instance? {ReferenceEquals(settings, settings2)}");

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("All Autoredi features demonstrated successfully!");
    }
}
