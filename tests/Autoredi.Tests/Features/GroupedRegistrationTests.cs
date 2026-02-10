using Autoredi.Tests.Autoredi;
using Autoredi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Features;

public class GroupedRegistrationTests
{
    [Test]
    public async Task AddAutorediServicesFirebase_OnlyRegistersFirebaseServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServicesFirebase();
        var provider = services.BuildServiceProvider();

        // Assert
        // Should have Firebase services
        await Assert.That(provider.GetService<FirebaseConfig>()).IsNotNull();
        await Assert.That(provider.GetService<FirebaseRepo>()).IsNotNull();
        await Assert.That(provider.GetService<FirebaseLogger>()).IsNotNull();

        // Should NOT have Account or Default services
        await Assert.That(provider.GetService<AccountService>()).IsNull();
        await Assert.That(provider.GetService<DefaultService>()).IsNull();
    }

    [Test]
    public async Task AddAutorediServicesAccount_OnlyRegistersAccountServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServicesAccount();
        var provider = services.BuildServiceProvider();

        // Assert
        await Assert.That(provider.GetService<AccountService>()).IsNotNull();

        // Should NOT have others
        await Assert.That(provider.GetService<FirebaseConfig>()).IsNull();
        await Assert.That(provider.GetService<DefaultService>()).IsNull();
    }

    [Test]
    public async Task AddAutorediServicesAutorediTests_OnlyRegistersUngroupedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServicesAutorediTests();
        var provider = services.BuildServiceProvider();

        // Assert
        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();

        // Should NOT have others
        await Assert.That(provider.GetService<FirebaseConfig>()).IsNull();
        await Assert.That(provider.GetService<AccountService>()).IsNull();
    }

    [Test]
    public async Task AddAutorediServices_RegistersAllGroups()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Assert - All services available
        await Assert.That(provider.GetService<FirebaseConfig>()).IsNotNull();
        await Assert.That(provider.GetService<AccountService>()).IsNotNull();
        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();
    }

    [Test]
    public async Task Priority_RegistersServicesInDescendingOrder()
    {
        // We verify order by inspecting the ServiceCollection directly.
        // Expected order for Firebase group:
        // 1. FirebaseConfig (Priority 100)
        // 2. FirebaseRepo (Priority 10)
        // 3. FirebaseLogger (Priority 0)

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutorediServicesFirebase();

        // Assert
        var descriptors = services.ToList();
        
        // Find indices
        var configIndex = descriptors.FindIndex(d => d.ServiceType == typeof(FirebaseConfig));
        var repoIndex = descriptors.FindIndex(d => d.ServiceType == typeof(FirebaseRepo));
        var loggerIndex = descriptors.FindIndex(d => d.ServiceType == typeof(FirebaseLogger));

        await Assert.That(configIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(repoIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(loggerIndex).IsGreaterThanOrEqualTo(0);

        await Assert.That(configIndex).IsLessThan(repoIndex);
        await Assert.That(repoIndex).IsLessThan(loggerIndex);
    }
}
