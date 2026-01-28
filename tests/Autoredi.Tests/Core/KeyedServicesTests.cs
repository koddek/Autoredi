namespace Autoredi.Tests.Core;

public class KeyedServicesTests
{
    private readonly IServiceProvider _serviceProvider;

    public KeyedServicesTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    [Arguments(ServiceKeys.Email)]
    [Arguments(ServiceKeys.SMS)]
    [Arguments(ServiceKeys.Push)]
    public async Task ResolveKeyedService_ReturnsCorrectImplementation_WhenValidKeyProvided(string key)
    {
        // Arrange
        Type expectedType = key switch
        {
            ServiceKeys.Email => typeof(TestEmailSender),
            ServiceKeys.SMS => typeof(TestSmsSender),
            ServiceKeys.Push => typeof(TestPushSender),
            _ => throw new InvalidOperationException("Invalid key")
        };

        // Act
        var sender = _serviceProvider.GetKeyedService<ITestMessageSender>(key);

        // Assert
        await Assert.That(sender).IsNotNull();
        await Assert.That(sender).IsOfType(expectedType);
    }

    [Test]
    public async Task ResolveKeyedService_ReturnsNull_WhenInvalidKeyProvided()
    {
        // Arrange
        // Act
        var sender = _serviceProvider.GetKeyedService<ITestMessageSender>("invalid-key");

        // Assert
        await Assert.That(sender).IsNull();
    }

    [Test]
    public async Task ResolveKeyedService_ReturnsNull_WhenKeyIsEmpty()
    {
        // Arrange
        // Act
        var sender = _serviceProvider.GetKeyedService<ITestMessageSender>("");

        // Assert
        await Assert.That(sender).IsNull();
    }

    [Test]
    [Arguments(ServiceKeys.Email)]
    [Arguments(ServiceKeys.SMS)]
    public async Task KeyedService_CallsMethod_WhenInvoked(string key)
    {
        // Arrange
        var mockSender = Substitute.For<ITestMessageSender>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestMessageSender>(key, mockSender);
        var provider = services.BuildServiceProvider();

        // Act
        var sender = provider.GetKeyedService<ITestMessageSender>(key);
        sender.Send("Test message");

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).Count().IsEqualTo(1);
        mockSender.Received().Send("Test message");
    }

    [Test]
    public async Task ResolveAllKeyedServices_ReturnsAllImplementations_WhenQueried()
    {
        // Arrange
        // Act
        var emailSender = _serviceProvider.GetKeyedService<ITestMessageSender>(ServiceKeys.Email);
        var smsSender = _serviceProvider.GetKeyedService<ITestMessageSender>(ServiceKeys.SMS);
        var pushSender = _serviceProvider.GetKeyedService<ITestMessageSender>(ServiceKeys.Push);

        // Assert
        await Assert.That(emailSender).IsOfType(typeof(TestEmailSender));
        await Assert.That(smsSender).IsOfType(typeof(TestSmsSender));
        await Assert.That(pushSender).IsOfType(typeof(TestPushSender));
    }

    [Test]
    public async Task KeyedServices_AreSingleton_ByDefaultConfiguration()
    {
        // Arrange
        // Act
        var sender1 = _serviceProvider.GetKeyedService<ITestMessageSender>(ServiceKeys.Email);
        var sender2 = _serviceProvider.GetKeyedService<ITestMessageSender>(ServiceKeys.Email);

        // Assert
        await Assert.That(ReferenceEquals(sender1, sender2)).IsTrue();
    }
}