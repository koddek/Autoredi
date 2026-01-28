using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.KeyedServices.Constants;

namespace Samples.KeyedServices.Services;

/// <summary>
/// Push notification service implementation with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), ServiceKeys.Push)]
public class PushNotificationService : INotificationService
{
    public string Channel => "Push";

    public void Send(string message)
    {
        Console.WriteLine($"[PUSH] Sending: {message}");
        Console.WriteLine($"[PUSH] Device: Mobile App");
        Console.WriteLine($"[PUSH] Status: Notification pushed!");
    }
}
