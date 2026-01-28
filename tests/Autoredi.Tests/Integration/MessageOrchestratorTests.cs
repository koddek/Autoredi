namespace Autoredi.Tests.Integration;

public class MessageOrchestratorTests
{
    private readonly IServiceProvider _serviceProvider;

    public MessageOrchestratorTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddSingleton<Func<string, ITestMessageSender>>(sp => key =>
            sp.GetKeyedService<ITestMessageSender>(key));
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveMessageOrchestrator_ReturnsInstance_WhenRegistered()
    {
        // Arrange
        // Act
        var orchestrator = _serviceProvider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        // Assert
        await Assert.That(orchestrator).IsNotNull();
    }

    [Test]
    [Arguments(ServiceKeys.Email, "Email message")]
    [Arguments(ServiceKeys.SMS, "SMS message")]
    [Arguments(ServiceKeys.Push, "Push message")]
    public async Task MessageOrchestratorSend_CallsCorrectSender_WhenKeyIsValid(string key, string message)
    {
        // Arrange
        var mockSender = Substitute.For<ITestMessageSender>();
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<ITestMessageSender>(key, mockSender);
        services.AddSingleton<Func<string, ITestMessageSender>>(sp => k =>
            sp.GetKeyedService<ITestMessageSender>(k));
        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        // Act
        orchestrator.Send(key, message);

        // Assert
        await Assert.That(mockSender.ReceivedCalls()).Count().IsEqualTo(1);
        mockSender.Received().Send(message);
    }

    [Test]
    public async Task MessageOrchestratorSend_ThrowsException_WhenKeyIsInvalid()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        // Act & Assert
        await Assert.That(() => orchestrator.Send("invalid-key", "Test message"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task MessageOrchestrator_TryGetSender_ReturnsNull_WhenKeyIsInvalid()
    {
        // Arrange
        // Act
        var sender = _serviceProvider.GetRequiredService<TestServices.TestMessageOrchestrator>()
            .TryGetSender("invalid-key");

        // Assert
        await Assert.That(sender).IsNull();
    }

    [Test]
    public async Task MessageOrchestrator_TryGetSender_ReturnsSender_WhenKeyIsValid()
    {
        // Arrange
        // Act
        var emailSender = _serviceProvider.GetRequiredService<TestServices.TestMessageOrchestrator>()
            .TryGetSender(ServiceKeys.Email);
        var smsSender = _serviceProvider.GetRequiredService<TestServices.TestMessageOrchestrator>()
            .TryGetSender(ServiceKeys.SMS);

        // Assert
        await Assert.That(emailSender).IsOfType(typeof(TestEmailSender));
        await Assert.That(smsSender).IsOfType(typeof(TestSmsSender));
    }

    [Test]
    public async Task MessageOrchestrator_DelegatesToKeyedServices_Correctly()
    {
        // Arrange
        var emailMock = Substitute.For<ITestMessageSender>();
        var smsMock = Substitute.For<ITestMessageSender>();

        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.Email, emailMock);
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.SMS, smsMock);
        services.AddSingleton<Func<string, ITestMessageSender>>(sp => key =>
            sp.GetKeyedService<ITestMessageSender>(key));
        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        // Act
        orchestrator.Send(ServiceKeys.Email, "Via email");
        orchestrator.Send(ServiceKeys.SMS, "Via SMS");

        // Assert
        emailMock.Received().Send("Via email");
        smsMock.Received().Send("Via SMS");
    }
}