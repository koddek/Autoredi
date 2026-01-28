namespace Autoredi.Tests.Core;

public class ServiceLifetimeTests
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceLifetimeTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task RegisterSingletonService_ReturnsSameInstance_OnMultipleResolutions()
    {
        // Arrange
        // Act
        var settings1 = _serviceProvider.GetRequiredService<TestSettings>();
        var settings2 = _serviceProvider.GetRequiredService<TestSettings>();

        // Assert
        await Assert.That(settings1).IsNotNull();
        await Assert.That(settings2).IsNotNull();
        await Assert.That(ReferenceEquals(settings1, settings2)).IsTrue();
    }

    [Test]
    public async Task ResolveSingletonService_HasExpectedProperties_WhenRegistered()
    {
        // Arrange
        // Act
        var settings = _serviceProvider.GetRequiredService<TestSettings>();

        // Assert
        await Assert.That(settings).IsNotNull();
        await Assert.That(settings.ApplicationName).IsEqualTo("TestApp");
        await Assert.That(settings.Version).IsEqualTo(1);
    }

    [Test]
    public async Task RegisterTransientService_ReturnsDifferentInstances_OnEachResolution()
    {
        // Arrange
        // Act
        var service1 = _serviceProvider.GetRequiredService<ITestLogService>();
        var service2 = _serviceProvider.GetRequiredService<ITestLogService>();

        // Assert
        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(ReferenceEquals(service1, service2)).IsFalse();
    }

    [Test]
    public async Task ResolveTransientService_ReturnsCorrectType_WhenInterfaceRegistered()
    {
        // Arrange
        // Act
        var service = _serviceProvider.GetRequiredService<ITestLogService>();

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsOfType(typeof(TestLogService));
    }

    [Test]
    public async Task RegisterScopedService_ReturnsSameInstance_WithinSingleScope()
    {
        // Arrange
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        // Act
        var service1a = scope1.ServiceProvider.GetRequiredService<ITestSessionService>();
        var service1b = scope1.ServiceProvider.GetRequiredService<ITestSessionService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<ITestSessionService>();

        // Assert
        await Assert.That(service1a.SessionId).IsEqualTo(service1b.SessionId);
        await Assert.That(service1a.SessionId).IsNotEqualTo(service2.SessionId);
    }

    [Test]
    public async Task ResolveScopedService_ReturnsDifferentInstances_AcrossDifferentScopes()
    {
        // Arrange
        // Act
        ITestSessionService service1, service2;
        using (var scope1 = _serviceProvider.CreateScope())
        {
            service1 = scope1.ServiceProvider.GetRequiredService<ITestSessionService>();
        }

        using (var scope2 = _serviceProvider.CreateScope())
        {
            service2 = scope2.ServiceProvider.GetRequiredService<ITestSessionService>();
        }

        // Assert
        await Assert.That(service1.SessionId).IsNotEqualTo(service2.SessionId);
    }

    [Test]
    public async Task RegisterMixedLifetimes_SatisfiesAllResolutions_InSingleContainer()
    {
        // Arrange
        // Act
        var singleton = _serviceProvider.GetRequiredService<TestSettings>();
        var transient1 = _serviceProvider.GetRequiredService<ITestLogService>();
        var transient2 = _serviceProvider.GetRequiredService<ITestLogService>();

        using var scope = _serviceProvider.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<ITestSessionService>();

        // Assert
        await Assert.That(singleton).IsNotNull();
        await Assert.That(transient1).IsNotNull();
        await Assert.That(transient2).IsNotNull();
        await Assert.That(scoped).IsNotNull();
        await Assert.That(ReferenceEquals(transient1, transient2)).IsFalse();
    }

    [Test]
    public async Task ResolveService_ThrowsException_WhenServiceNotRegistered()
    {
        // Arrange
        // Act & Assert
        await Assert.That(() => _serviceProvider.GetRequiredService<ITestExternalService>())
            .ThrowsException();
    }
}