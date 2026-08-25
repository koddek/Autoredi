namespace Autoredi.Tests.Features;

public class RegistrationSemanticsTests
{
    [Test]
    public async Task MultiInterface_RegistersEachServiceType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();

        // Act
        var provider = services.BuildServiceProvider();
        var audit = provider.GetService<IAuditTrail>();
        var telemetry = provider.GetService<ITelemetrySink>();

        // Assert
        await Assert.That(audit).IsNotNull();
        await Assert.That(telemetry).IsNotNull();
    }

    [Test]
    public async Task AddAutorediServices_IsIdempotent_OnRepeatedCalls()
    {
        // Arrange
        var once = new ServiceCollection().AddAutorediServices();
        var twice = new ServiceCollection().AddAutorediServices().AddAutorediServices();

        // Assert - TryAdd semantics: a second call adds no duplicate descriptors.
        await Assert.That(twice.Count).IsEqualTo(once.Count);
    }

    [Test]
    public async Task AddAutorediServices_NeverOverridesManualRegistrations()
    {
        // Arrange - use a real instance as manual registration; the point is identity preservation, not mocking behavior
        var manualInstance = new DefaultService();
        var services = new ServiceCollection();
        services.AddSingleton<DefaultService>(manualInstance);

        // Act - generated registration must not replace the existing descriptor.
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<DefaultService>();

        // Assert
        await Assert.That(ReferenceEquals(resolved, manualInstance)).IsTrue();
    }
}
