namespace Autoredi.Tests.Features;

public class RegistrationSemanticsTests
{
    [Test]
    public async Task AddAutorediServices_ReturnsTheSameCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddAutorediServices();

        await Assert.That(ReferenceEquals(services, result)).IsTrue();
    }

    [Test]
    public async Task MultiInterface_RegistersEachServiceType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();

        // Act
        using var provider = services.BuildServiceProvider();
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
        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<DefaultService>();

        // Assert
        await Assert.That(ReferenceEquals(resolved, manualInstance)).IsTrue();
    }

    [Test]
    public async Task ManualFactoryRegistration_KeepsWinningOverGeneratedInterfaceRegistration()
    {
        // A factory registration has no implementation type, so it counts as a hand-written
        // decision exactly like an instance registration does.
        var manual = new TestLogService();
        var services = new ServiceCollection();
        services.AddSingleton<ITestLogService>(_ => manual);

        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetServices<ITestLogService>().Count()).IsEqualTo(1);
        await Assert.That(ReferenceEquals(provider.GetRequiredService<ITestLogService>(), manual)).IsTrue();
    }

    [Test]
    public async Task ManualTypeRegistration_ForAnotherImplementation_StillCoexists()
    {
        // A hand-written registration that names its implementation type is just another
        // implementation, so it must not suppress the generated one.
        var services = new ServiceCollection();
        services.AddTransient<ITestLogService, ManualLogService>();

        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetServices<ITestLogService>().ToList();
        await Assert.That(resolved.Select(s => s.GetType()))
            .IsEquivalentTo(new[] { typeof(TestLogService), typeof(ManualLogService) });
    }
}
