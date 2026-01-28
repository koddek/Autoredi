using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.KeyedServices.Constants;

namespace Samples.KeyedServices.Services;

/// <summary>
/// SMS notification service implementation with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), ServiceKeys.SMS)]
public class SmsNotificationService : INotificationService
{
    public string Channel => "SMS";

    public void Send(string message)
    {
        Console.WriteLine($"[SMS] Sending: {message}");
        Console.WriteLine($"[SMS] To: +1234567890");
        Console.WriteLine($"[SMS] Status: Delivered!");
    }
}
