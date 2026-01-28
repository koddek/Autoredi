using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.Complete.Services;

namespace Samples.Complete.Controllers;

/// <summary>
/// Controller demonstrating keyed service injection using FromKeyedServices.
/// </summary>
[Autoredi]
public class NotificationController
{
    private readonly INotificationService _primaryNotification;
    private readonly ILogger _logger;

    public NotificationController(
        [FromKeyedServices(NotificationKeys.Email)] INotificationService primaryNotification,
        ILogger logger)
    {
        _primaryNotification = primaryNotification;
        _logger = logger;
    }

    public void NotifyUser(string userId, string message)
    {
        _logger.Log($"Notifying user {userId}");
        _primaryNotification.Send(message);
    }
}
