using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Fixtures;

/// <summary>
/// Concrete service without interface - self-registered
/// </summary>
[Autoredi(ServiceLifetime.Singleton)]
public class TestSettings
{
    public string ApplicationName => "TestApp";
    public int Version => 1;
}

/// <summary>
/// Interface and implementation for transient service
/// </summary>
public interface ITestLogService
{
    void Log(string message);
}

[Autoredi(ServiceLifetime.Transient, typeof(ITestLogService))]
public class TestLogService : ITestLogService
{
    public void Log(string message)
    {
        // Implementation for testing
    }
}

/// <summary>
/// Interface and implementation for scoped service
/// </summary>
public interface ITestSessionService
{
    Guid SessionId { get; }
}

[Autoredi(ServiceLifetime.Scoped, typeof(ITestSessionService))]
public class TestSessionService : ITestSessionService
{
    public Guid SessionId { get; } = Guid.NewGuid();
}

/// <summary>
/// Interface with multiple keyed implementations
/// </summary>
public interface ITestMessageSender
{
    void Send(string message);
}

[Autoredi(ServiceLifetime.Singleton, typeof(ITestMessageSender), ServiceKeys.Email)]
public class TestEmailSender : ITestMessageSender
{
    public void Send(string message)
    {
        // Email sending implementation
    }
}

[Autoredi(ServiceLifetime.Singleton, typeof(ITestMessageSender), ServiceKeys.SMS)]
public class TestSmsSender : ITestMessageSender
{
    public void Send(string message)
    {
        // SMS sending implementation
    }
}

[Autoredi(ServiceLifetime.Singleton, typeof(ITestMessageSender), ServiceKeys.Push)]
public class TestPushSender : ITestMessageSender
{
    public void Send(string message)
    {
        // Push notification implementation
    }
}

/// <summary>
/// Interface without Autoredi attribute - for testing registration scenarios
/// </summary>
public interface ITestExternalService
{
    string GetData();
}

public class TestExternalService : ITestExternalService
{
    public string GetData() => "External data";
}

/// <summary>
/// Controller with dependency injection
/// </summary>
public static class TestServices
{
    [Autoredi]
    public class TestMessageController
    {
        private readonly ITestMessageSender _sender;

        public TestMessageController([FromKeyedServices(ServiceKeys.SMS)] ITestMessageSender sender)
        {
            _sender = sender;
        }

        public void SendMessage(string message)
        {
            _sender.Send(message);
        }

        public ITestMessageSender GetSender() => _sender;
    }

    [Autoredi]
    public class TestMessageOrchestrator
    {
        private readonly Func<string, ITestMessageSender> _resolver;

        public TestMessageOrchestrator(Func<string, ITestMessageSender> resolver)
        {
            _resolver = resolver;
        }

        public void Send(string key, string message)
        {
            var sender = _resolver(key) ?? throw new InvalidOperationException("Invalid service key.");
            sender.Send(message);
        }

        public ITestMessageSender? TryGetSender(string key) => _resolver(key);
    }
}