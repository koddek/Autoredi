namespace Autoredi.Tests;

public class LogServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public LogServiceTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveLogService_ReturnsInstance_WhenRegisteredAsTransient()
    {
        // Arrange
        // Act
        var logService = _serviceProvider.GetRequiredService<ILogService>();

        // Assert
        await Assert.That(logService).IsNotNull();
        await Assert.That(logService).IsAssignableTo<LogService>();
    }

    [Test]
    public async Task ResolveLogService_ReturnsDifferentInstances_WhenResolvedMultipleTimes()
    {
        // Arrange
        // Act
        var logService1 = _serviceProvider.GetRequiredService<ILogService>();
        var logService2 = _serviceProvider.GetRequiredService<ILogService>();

        // Assert
        await Assert.That(logService1).IsNotEqualTo(logService2);
    }

    [Test]
    public async Task Log_CallsMethod_WhenInvoked()
    {
        // Arrange
        var mockLogService = Substitute.For<ILogService>();
        var services = new ServiceCollection();
        services.AddSingleton<ILogService>(mockLogService);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var logService = serviceProvider.GetRequiredService<ILogService>();
        logService.Log("Test log");

        // Assert
        await Assert.That(mockLogService.ReceivedCalls()).HasCount(1);
        mockLogService.Received().Log("Test log");
    }
}
