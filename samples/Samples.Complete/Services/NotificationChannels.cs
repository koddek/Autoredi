using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;

namespace Samples.Complete.Services;

/// <summary>
/// Service keys for notification channels.
/// </summary>
public static class NotificationKeys
{
    public const string Email = "email";
    public const string SMS = "sms";
    public const string Slack = "slack";
}

/// <summary>
/// Email notification service with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), NotificationKeys.Email)]
public class EmailNotifier : INotificationService
{
    public string Channel => "Email";

    public void Send(string message)
    {
        Console.WriteLine($"[EMAIL] Sending: {message}");
    }
}

/// <summary>
/// SMS notification service with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), NotificationKeys.SMS)]
public class SmsNotifier : INotificationService
{
    public string Channel => "SMS";

    public void Send(string message)
    {
        Console.WriteLine($"[SMS] Sending: {message}");
    }
}

/// <summary>
/// Slack notification service with keyed registration.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), NotificationKeys.Slack)]
public class SlackNotifier : INotificationService
{
    public string Channel => "Slack";

    public void Send(string message)
    {
        Console.WriteLine($"[SLACK] Sending: {message}");
    }
}
