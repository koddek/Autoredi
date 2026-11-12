using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Models;

namespace Autoredi.Tests.Features;

/// <summary>
/// Runtime tests for open generic service registrations (typeof(IFoo&lt;&gt;)).
/// The test assembly's own generated extension class carries the registrations.
/// </summary>
public class OpenGenericRegistrationTests
{
    [Test]
    public async Task Resolve_SingleService_UsesLastRegistrationOrderForOpenGenerics()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var repo = provider.GetRequiredService<IOpenRepo<User>>();

        await Assert.That(repo).IsNotNull();
        await Assert.That(repo).IsOfType(typeof(OpenRepo<User>));
    }

    [Test]
    public async Task Resolve_EnumerableService_KeepsEveryOpenGenericImplementation()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var repos = provider.GetServices<IOpenRepo<User>>().ToList();

        await Assert.That(repos.Count).IsEqualTo(2);
        await Assert.That(repos.Any(r => r is OpenRepo<User>)).IsTrue();
        await Assert.That(repos.Any(r => r is AuditedOpenRepo<User>)).IsTrue();
    }

    [Test]
    public async Task DoubleCall_AddsNoDuplicateOpenGenericDescriptors()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var repos = provider.GetServices<IOpenRepo<User>>().ToList();

        await Assert.That(repos.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Resolve_KeyedService_UsesTheKeyedOpenGenericImplementation()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var repo = provider.GetKeyedService<IOpenRepo<User>>(ServiceKeys.Email);

        await Assert.That(repo).IsNotNull();
        await Assert.That(repo).IsOfType(typeof(KeyedOpenRepo<User>));
    }

    [Test]
    public async Task Resolve_HandWrittenRegistrationFirst_GeneratedRegistrationDoesNotReplaceIt()
    {
        var services = new ServiceCollection();
        services.AddScoped(typeof(IOpenRepo<>), typeof(OpenRepo<>));   // hand-written first
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var descriptors = services.Where(d => d.ServiceType == typeof(IOpenRepo<>) && d.ImplementationType == typeof(OpenRepo<>)).ToList();

        await Assert.That(descriptors.Count).IsEqualTo(1);
    }
}
