using Autoredi.Attributes;

namespace Autoredi.Tests;

// Sample types for testing Autoredi
public static class ServiceKeys
{
    public const string Email = "email";
    public const string SMS = "sms";
}

// Concrete service without interface
[Autoredi(ServiceLifetime.Singleton)]
public class Settings
{
    public string ApplicationName => "TestApp";
}

// Interface and single implementation
public interface ILogService
{
    void Log(string message);
}

[Autoredi(ServiceLifetime.Transient, typeof(ILogService))]
public class LogService : ILogService
{
    public void Log(string message)
    {
        // Implementation not tested; mocked in tests
    }
}

// Interface with multiple keyed implementations
public interface IMessageSender
{
    void Send(string message);
}

[Autoredi(ServiceLifetime.Singleton, typeof(IMessageSender), ServiceKeys.Email)]
public class EmailSender : IMessageSender
{
    public void Send(string message)
    {
        // Implementation not tested
    }
}

[Autoredi(ServiceLifetime.Singleton, typeof(IMessageSender), ServiceKeys.SMS)]
public class SmsSender : IMessageSender
{
    public void Send(string message)
    {
        // Implementation not tested
    }
}

// Controller and orchestrator for complex scenarios
public static class Services
{
    [Autoredi]
    public class MessageController
    {
        private readonly IMessageSender _sender;

        public MessageController([FromKeyedServices(ServiceKeys.SMS)] IMessageSender sender)
        {
            _sender = sender;
        }

        public void SendMessage(string message)
        {
            _sender.Send(message);
        }
    }

    [Autoredi]
    public class MessageOrchestrator
    {
        private readonly Func<string, IMessageSender> _resolver;

        public MessageOrchestrator(Func<string, IMessageSender> resolver)
        {
            _resolver = resolver;
        }

        public void Send(string key, string message)
        {
            var sender = _resolver(key) ?? throw new InvalidOperationException("Invalid service key.");
            sender.Send(message);
        }
    }
}
