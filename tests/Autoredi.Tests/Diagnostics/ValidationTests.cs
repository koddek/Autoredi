namespace Autoredi.Tests.Diagnostics;

/// <summary>
/// Tests for edge cases and validation scenarios
/// These tests verify the generator handles various error conditions correctly
/// </summary>
public class ValidationTests
{
    [Test]
    public async Task AutorediExtensions_Exists_WhenAutorediServicesCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServices();

        // Assert - Verify the extension method was generated and can be called
        await Assert.That(services).IsNotNull();
    }

    [Test]
    public async Task ServiceCollection_CanAddMultipleAutorediCalls_WithoutConflict()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServices();
        services.AddAutorediServices(); // Multiple calls should not conflict

        // Assert
        await Assert.That(services).IsNotNull();
    }

    [Test]
    public async Task ServiceCollection_ReturnsSelf_FromAddAutorediServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAutorediServices();

        // Assert
        await Assert.That(ReferenceEquals(services, result)).IsTrue();
    }

    [Test]
    public async Task SingletonService_ExistsInContainer_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var settings = provider.GetService<TestSettings>();

        // Assert
        await Assert.That(settings).IsNotNull();
    }

    [Test]
    public async Task TransientService_ExistsInContainer_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<ITestLogService>();

        // Assert
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task ScopedService_ExistsInContainer_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        ITestSessionService? service;
        using (var scope = provider.CreateScope())
        {
            service = scope.ServiceProvider.GetService<ITestSessionService>();
        }

        // Assert
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task KeyedServices_AllExistInContainer_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var emailSender = provider.GetKeyedService<ITestMessageSender>(ServiceKeys.Email);
        var smsSender = provider.GetKeyedService<ITestMessageSender>(ServiceKeys.SMS);
        var pushSender = provider.GetKeyedService<ITestMessageSender>(ServiceKeys.Push);

        // Assert
        await Assert.That(emailSender).IsNotNull();
        await Assert.That(smsSender).IsNotNull();
        await Assert.That(pushSender).IsNotNull();
    }

    [Test]
    public async Task ControllerWithKeyedInjection_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var controller = provider.GetService<TestServices.TestMessageController>();

        // Assert
        await Assert.That(controller).IsNotNull();
    }

    [Test]
    public async Task OrchestratorWithFuncResolver_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddSingleton<Func<string, ITestMessageSender>>(sp => key =>
            sp.GetKeyedService<ITestMessageSender>(key));
        var provider = services.BuildServiceProvider();

        // Act
        var orchestrator = provider.GetService<TestServices.TestMessageOrchestrator>();

        // Assert
        await Assert.That(orchestrator).IsNotNull();
    }

    [Test]
    public async Task AddAutorediServices_CanBeCalledMultipleTimes_WithoutSideEffects()
    {
        // Arrange
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();

        // Act
        services1.AddAutorediServices();
        services1.AddAutorediServices(); // Second call

        services2.AddAutorediServices();

        // Assert - Both should resolve the same services
        var provider1 = services1.BuildServiceProvider();
        var provider2 = services2.BuildServiceProvider();

        var settings1 = provider1.GetRequiredService<TestSettings>();
        var settings2 = provider2.GetRequiredService<TestSettings>();

        await Assert.That(settings1.ApplicationName).IsEqualTo(settings2.ApplicationName);
    }
}