using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.KeyedServices.Constants;

namespace Samples.KeyedServices.Services;

/// <summary>
/// Email notification service implementation with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), ServiceKeys.Email)]
public class EmailNotificationService : INotificationService
{
    public string Channel => "Email";

    public void Send(string message)
    {
        Console.WriteLine($"[EMAIL] Sending: {message}");
        Console.WriteLine($"[EMAIL] To: recipient@example.com");
        Console.WriteLine($"[EMAIL] Status: Sent successfully!");
    }
}
