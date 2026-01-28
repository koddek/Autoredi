namespace Autoredi.Tests.Core;

public class ServiceResolutionTests
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceResolutionTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveInterface_ReturnsImplementation_WhenMapped()
    {
        // Arrange
        // Act
        var service = _serviceProvider.GetRequiredService<ITestLogService>();

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsOfType(typeof(TestLogService));
    }

    [Test]
    public async Task ResolveConcreteType_ReturnsInstance_WhenSelfRegistered()
    {
        // Arrange
        // Act
        var settings = _serviceProvider.GetRequiredService<TestSettings>();

        // Assert
        await Assert.That(settings).IsNotNull();
        await Assert.That(settings.ApplicationName).IsEqualTo("TestApp");
    }

    [Test]
    public async Task ResolveService_CanInvokeMethod_WhenServiceIsValid()
    {
        // Arrange
        var mockService = Substitute.For<ITestLogService>();
        var services = new ServiceCollection();
        services.AddSingleton<ITestLogService>(mockService);
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetRequiredService<ITestLogService>();
        service.Log("Test message");

        // Assert
        await Assert.That(mockService.ReceivedCalls()).Count().IsEqualTo(1);
        mockService.Received().Log("Test message");
    }

    [Test]
    public async Task ResolveMultipleServices_ReturnsCorrectTypes_WhenRequested()
    {
        // Arrange
        // Act
        var logService = _serviceProvider.GetRequiredService<ITestLogService>();
        var settings = _serviceProvider.GetRequiredService<TestSettings>();

        // Assert
        await Assert.That(logService).IsOfType(typeof(TestLogService));
        await Assert.That(settings).IsOfType(typeof(TestSettings));
    }

    [Test]
    public async Task ResolveService_AfterDisposal_ReturnsNewInstance_WhenTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        ITestLogService service1, service2;
        using (var scope1 = provider.CreateScope())
        {
            service1 = scope1.ServiceProvider.GetRequiredService<ITestLogService>();
        }

        using (var scope2 = provider.CreateScope())
        {
            service2 = scope2.ServiceProvider.GetRequiredService<ITestLogService>();
        }

        // Assert - Transient services should be different instances
        await Assert.That(ReferenceEquals(service1, service2)).IsFalse();
    }

    [Test]
    public async Task GetService_ReturnsNull_WhenServiceNotRegistered()
    {
        // Arrange
        // Act
        var service = _serviceProvider.GetService<ITestExternalService>();

        // Assert
        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task GetRequiredService_ThrowsException_WhenServiceNotRegistered()
    {
        // Arrange
        // Act & Assert
        await Assert.That(() => _serviceProvider.GetRequiredService<ITestExternalService>())
            .ThrowsException();
    }

    [Test]
    public async Task ResolveService_WithinNestedScope_WorksCorrectly()
    {
        // Arrange
        // Act
        TestSettings outerSettings;
        ITestLogService outerLogService;

        using (var outerScope = _serviceProvider.CreateScope())
        {
            outerSettings = outerScope.ServiceProvider.GetRequiredService<TestSettings>();
            outerLogService = outerScope.ServiceProvider.GetRequiredService<ITestLogService>();

            using (var innerScope = outerScope.ServiceProvider.CreateScope())
            {
                var innerSettings = innerScope.ServiceProvider.GetRequiredService<TestSettings>();
                var innerLogService = innerScope.ServiceProvider.GetRequiredService<ITestLogService>();

                // Assert - Singleton should be same across scopes
                await Assert.That(ReferenceEquals(outerSettings, innerSettings)).IsTrue();

                // Assert - Transient should be different
                await Assert.That(ReferenceEquals(outerLogService, innerLogService)).IsFalse();
            }
        }
    }
}