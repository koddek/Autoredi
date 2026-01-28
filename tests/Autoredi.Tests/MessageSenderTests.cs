namespace Autoredi.Tests;

public class MessageSenderTests
{
    private readonly IServiceProvider _serviceProvider;

    public MessageSenderTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    [Arguments(ServiceKeys.Email)]
    [Arguments(ServiceKeys.SMS)]
    public async Task ResolveKeyedMessageSender_ReturnsCorrectInstance_WhenKeyIsValid(string key)
    {
        // Arrange
        Type expectedType = key switch
        {
            ServiceKeys.Email => typeof(EmailSender),
            ServiceKeys.SMS => typeof(SmsSender),
            _ => throw new InvalidOperationException("Invalid key")
        };

        // Act
        var sender = _serviceProvider.GetKeyedService<IMessageSender>(key);

        // Assert
        await Assert.That(sender).IsNotNull();
        await Assert.That(sender).IsOfType(expectedType);
    }

    [Test]
    public async Task ResolveKeyedMessageSender_ReturnsNull_WhenKeyIsInvalid()
    {
        // Arrange
        // Act
        var sender = _serviceProvider.GetKeyedService<IMessageSender>("invalid");

        // Assert
        await Assert.That(sender).IsNull();
    }

    [Test]
    [Arguments(ServiceKeys.Email, "Email message")]
    [Arguments(ServiceKeys.SMS, "SMS message")]
    public async Task Send_CallsMethod_WhenInvokedWithValidKey(string key, string message)
    {
        // Arrange
        var mockSender = Substitute.For<IMessageSender>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IMessageSender>(key, mockSender);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var sender = serviceProvider.GetKeyedService<IMessageSender>(key);
        sender.Send(message);

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).HasCount(1);
        mockSender.Received().Send(message);
    }
}
