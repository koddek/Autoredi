namespace Samples.Common.Interfaces;

/// <summary>
/// Notification service interface for demonstrating keyed services.
/// </summary>
public interface INotificationService
{
    void Send(string message);
    string Channel { get; }
}
