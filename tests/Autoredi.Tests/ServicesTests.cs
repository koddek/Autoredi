namespace Autoredi.Tests;

public class ServicesTests
{
    private readonly IServiceProvider _serviceProvider;

    public ServicesTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddSingleton<Func<string, IMessageSender>>(sp => key =>
            sp.GetKeyedService<IMessageSender>(key));
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveMessageController_ReturnsInstance_WhenRegistered()
    {
        // Arrange
        // Act
        var controller = _serviceProvider.GetRequiredService<Services.MessageController>();

        // Assert
        await Assert.That(controller).IsNotNull();
    }

    [Test]
    public async Task MessageControllerSendMessage_CallsSender_WhenInvoked()
    {
        // Arrange
        var mockSender = Substitute.For<IMessageSender>();
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<IMessageSender>(ServiceKeys.SMS, mockSender);
        var serviceProvider = services.BuildServiceProvider();
        var controller = serviceProvider.GetRequiredService<Services.MessageController>();

        // Act
        controller.SendMessage("Controller message");

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).HasCount(1);
        mockSender.Received().Send("Controller message");
    }

    [Test]
    public async Task ResolveMessageOrchestrator_ReturnsInstance_WhenRegistered()
    {
        // Arrange
        // Act
        var orchestrator = _serviceProvider.GetRequiredService<Services.MessageOrchestrator>();

        // Assert
        await Assert.That(orchestrator).IsNotNull();
    }

    [Test]
    [Arguments(ServiceKeys.SMS, "Orchestrator message")]
    public async Task MessageOrchestratorSend_CallsSender_WhenKeyIsValid(string key, string message)
    {
        // Arrange
        var mockSender = Substitute.For<IMessageSender>();
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<IMessageSender>(key, mockSender);
        services.AddSingleton<Func<string, IMessageSender>>(sp => k =>
            sp.GetKeyedService<IMessageSender>(k));
        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<Services.MessageOrchestrator>();

        // Act
        orchestrator.Send(key, message);

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).HasCount(1);
        mockSender.Received().Send(message);
    }

    [Test]
    public async Task MessageOrchestratorSend_ThrowsException_WhenKeyIsInvalid()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<Services.MessageOrchestrator>();

        // Act & Assert
        await Assert.That(() => orchestrator.Send("invalid", "Test")).ThrowsExactly<InvalidOperationException>();
    }
}
