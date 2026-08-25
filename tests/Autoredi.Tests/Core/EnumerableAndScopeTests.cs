namespace Autoredi.Tests.Core;

public class EnumerableAndScopeTests
{
    [Test]
    public async Task ResolveEnumerable_ReturnsAllImplementations_WhenMultipleNonKeyedRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var generators = provider.GetServices<IReportGenerator>().ToList();

        // Assert - TryAddEnumerable allows multiple implementations of same interface
        await Assert.That(generators.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(generators.OfType<PdfReportGenerator>().Any()).IsTrue();
        await Assert.That(generators.OfType<CsvReportGenerator>().Any()).IsTrue();
    }

    [Test]
    public async Task ResolveEnumerable_ReturnsDistinctTransientInstances_OnEachResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var first = provider.GetServices<IReportGenerator>().OfType<PdfReportGenerator>().First();
        var second = provider.GetServices<IReportGenerator>().OfType<PdfReportGenerator>().First();

        // Assert - Transient should give new instance each enumeration? Actually GetServices creates new scope's instances, but within same provider transient returns new each call
        await Assert.That(ReferenceEquals(first, second)).IsFalse();
    }

    [Test]
    public async Task ScopedKeyedService_ReturnsSameInstance_WithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        IScopedKeyedService a1, a2;
        using (var scope = provider.CreateScope())
        {
            a1 = scope.ServiceProvider.GetRequiredKeyedService<IScopedKeyedService>("scoped-a");
            a2 = scope.ServiceProvider.GetRequiredKeyedService<IScopedKeyedService>("scoped-a");
        }

        // Assert
        await Assert.That(a1.Id).IsEqualTo(a2.Id);
    }

    [Test]
    public async Task ScopedKeyedService_ReturnsDifferentInstances_AcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        IScopedKeyedService s1, s2;
        using (var scope1 = provider.CreateScope())
            s1 = scope1.ServiceProvider.GetRequiredKeyedService<IScopedKeyedService>("scoped-a");
        using (var scope2 = provider.CreateScope())
            s2 = scope2.ServiceProvider.GetRequiredKeyedService<IScopedKeyedService>("scoped-a");

        // Assert
        await Assert.That(s1.Id).IsNotEqualTo(s2.Id);
    }

    [Test]
    public async Task TransientKeyedService_ReturnsDifferentInstances_EachResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var t1 = provider.GetRequiredKeyedService<IScopedKeyedService>("transient-b");
        var t2 = provider.GetRequiredKeyedService<IScopedKeyedService>("transient-b");

        // Assert
        await Assert.That(t1.Id).IsNotEqualTo(t2.Id);
    }

    [Test]
    public async Task SpecialCharacterKey_ResolvesCorrectly()
    {
        // Arrange - service key contains quote and backslash, verifies FormatLiteral escaping
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetKeyedService<ISpecialKeyService>("quote\"and\\slash");

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service!.Handle()).IsEqualTo("special");
    }

    [Test]
    public async Task KeyedServices_AreDistinctPerKey_EvenSameInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // Act
        var scoped = provider.GetKeyedService<IScopedKeyedService>("scoped-a");
        var transient = provider.GetKeyedService<IScopedKeyedService>("transient-b");
        var special = provider.GetKeyedService<ISpecialKeyService>("quote\"and\\slash");

        // Assert - different keys, different implementations, no cross-talk
        await Assert.That(scoped).IsNotNull();
        await Assert.That(transient).IsNotNull();
        await Assert.That(special).IsNotNull();
        await Assert.That(scoped!.GetType()).IsNotEqualTo(transient!.GetType());
    }
}
