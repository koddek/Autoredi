namespace Autoredi.Tests.Integration;

public class MessageControllerTests
{
    private readonly IServiceProvider _serviceProvider;

    public MessageControllerTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveMessageController_ReturnsInstance_WhenRegistered()
    {
        // Arrange
        // Act
        var controller = _serviceProvider.GetRequiredService<TestServices.TestMessageController>();

        // Assert
        await Assert.That(controller).IsNotNull();
    }

    [Test]
    public async Task MessageController_ReceivesSmsSender_WhenResolved()
    {
        // Arrange
        // Act
        var controller = _serviceProvider.GetRequiredService<TestServices.TestMessageController>();
        var sender = controller.GetSender();

        // Assert
        await Assert.That(sender).IsNotNull();
        await Assert.That(sender).IsOfType(typeof(TestSmsSender));
    }

    [Test]
    public async Task MessageControllerSendMessage_CallsSmsSender_WhenInvoked()
    {
        // Arrange
        var mockSender = Substitute.For<ITestMessageSender>();
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.SMS, mockSender);
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<TestServices.TestMessageController>();

        // Act
        controller.SendMessage("Test message");

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).Count().IsEqualTo(1);
        mockSender.Received().Send("Test message");
    }

    [Test]
    public async Task MessageController_ImplementsDependencyInjection_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var controller1 = provider.GetRequiredService<TestServices.TestMessageController>();
        var controller2 = provider.GetRequiredService<TestServices.TestMessageController>();

        // Assert - Controllers are transient, so they should be different instances
        await Assert.That(ReferenceEquals(controller1, controller2)).IsFalse();

        // But they should receive the same SMS sender instance (singleton)
        await Assert.That(ReferenceEquals(controller1.GetSender(), controller2.GetSender())).IsTrue();
    }
}