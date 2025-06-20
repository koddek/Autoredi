using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests;

public class SettingsTests
{
    private readonly IServiceProvider _serviceProvider;

    public SettingsTests()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task ResolveSettings_ReturnsInstance_WhenRegisteredAsSingleton()
    {
        // Arrange
        // Act
        var settings = _serviceProvider.GetRequiredService<Settings>();

        // Assert
        await Assert.That(settings).IsNotNull();
        await Assert.That(settings.ApplicationName).IsEqualTo("TestApp");
    }

    [Test]
    public async Task ResolveSettings_ReturnsSameInstance_WhenResolvedMultipleTimes()
    {
        // Arrange
        // Act
        var settings1 = _serviceProvider.GetRequiredService<Settings>();
        var settings2 = _serviceProvider.GetRequiredService<Settings>();

        var result = settings1.Equals(settings2);

        // Assert
        await Assert.That(result).IsTrue();
    }
}
