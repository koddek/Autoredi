using Samples.Modular.Infrastructure.Services;

namespace Autoredi.Tests.Features;

public class AdvancedRegistrationTests
{
    [Test]
    public async Task AssemblyWide_RegistersEveryGroupIncludingDefault()
    {
        // AddAutorediServicesAutorediTests must include Firebase + Account + Default
        var services = new ServiceCollection();
        services.AddAutorediServicesAutorediTests();
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetService<FirebaseConfig>()).IsNotNull();
        await Assert.That(provider.GetService<AccountService>()).IsNotNull();
        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();
        await Assert.That(provider.GetService<IReportGenerator>()).IsNotNull();
        await Assert.That(provider.GetServices<IReportGenerator>().Count()).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task DefaultOnly_DoesNotIncludeGroupedServices()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();
        await Assert.That(provider.GetService<FirebaseConfig>()).IsNull();
        await Assert.That(provider.GetService<AccountService>()).IsNull();
        // PdfReportGenerator is default-group but registered as IReportGenerator, not as self
        await Assert.That(provider.GetService<IReportGenerator>()).IsNotNull();
        await Assert.That(provider.GetServices<IReportGenerator>().Count()).IsGreaterThanOrEqualTo(2);
        await Assert.That(provider.GetService<AlphaService>()).IsNull();
    }

    [Test]
    public async Task CrossAssembly_All_IncludesInfrastructureServices()
    {
        // Verifies marker-probe path: tests references Samples.Modular.Infrastructure
        var services = new ServiceCollection();
        services.AddAutorediServicesAll();
        var provider = services.BuildServiceProvider();

        // Local services still present
        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();
        // Infrastructure services become visible via All
        await Assert.That(provider.GetService<FirebaseCore>()).IsNotNull();
        await Assert.That(provider.GetService<DatabaseService>()).IsNotNull();
    }

    [Test]
    public async Task CrossAssembly_SelectiveGroupViaAliasedCall_DoesNotLeakOtherGroups()
    {
        // Selective cross-assembly: app asks infrastructure for Storage only
        var services = new ServiceCollection();
        services.AddAutorediServices(); // app defaults only
        // Call infrastructure's generated extension statically via alias-like fully qualified
        global::Samples.Modular.Infrastructure.Autoredi.AutorediServiceCollectionExtensions.AddAutorediServicesStorage(services);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetService<DatabaseService>()).IsNotNull();
        await Assert.That(provider.GetService<FirebaseCore>()).IsNull();
        await Assert.That(provider.GetService<DefaultService>()).IsNotNull();
    }

    [Test]
    public async Task PriorityTieBreak_Alphabetical_WhenSamePriority()
    {
        // Alpha/Beta/Gamma all share Priority 5 in PriorityTie group -> alphabetical emission order
        var services = new ServiceCollection();
        services.AddAutorediServicesPriorityTie();
        var order = services.Select(d => d.ServiceType.Name).ToList();

        var alphaIdx = order.IndexOf(nameof(AlphaService));
        var betaIdx = order.IndexOf(nameof(BetaService));
        var gammaIdx = order.IndexOf(nameof(GammaService));

        await Assert.That(alphaIdx).IsGreaterThanOrEqualTo(0);
        await Assert.That(betaIdx).IsGreaterThanOrEqualTo(0);
        await Assert.That(gammaIdx).IsGreaterThanOrEqualTo(0);
        await Assert.That(alphaIdx).IsLessThan(betaIdx);
        await Assert.That(betaIdx).IsLessThan(gammaIdx);
    }

    [Test]
    public async Task SelfVsInterface_WhenInterfaceSpecified_SelfNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        // SelfCheckImpl is registered as ISelfCheck, not as SelfCheckImpl concrete
        await Assert.That(provider.GetService<ISelfCheck>()).IsNotNull();
        await Assert.That(provider.GetService<ISelfCheck>()).IsOfType(typeof(SelfCheckImpl));
        await Assert.That(provider.GetService<SelfCheckImpl>()).IsNull();
    }

    [Test]
    public async Task TryAddEnumerable_Keyed_IsIdempotent()
    {
        var s1 = new ServiceCollection();
        s1.AddAutorediServices();
        var c1 = s1.Count;

        var s2 = new ServiceCollection();
        s2.AddAutorediServices();
        s2.AddAutorediServices();
        var c2 = s2.Count;

        await Assert.That(c1).IsEqualTo(c2);

        // Also for keyed: two calls with same keyed registrations stay same count
        var k1 = new ServiceCollection();
        k1.AddAutorediServices();
        var kc1 = k1.Count;

        var k2 = new ServiceCollection();
        k2.AddAutorediServices();
        k2.AddAutorediServices();
        var kc2 = k2.Count;

        await Assert.That(kc1).IsEqualTo(kc2);
    }

    [Test]
    public async Task TryAddEnumerable_ManualKeyedSingleton_NotOverridden()
    {
        var imposter = ITestMessageSender.Imposter();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.Email, imposter.Instance());
        services.AddAutorediServices(); // should not replace Email keyed registration
        var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredKeyedService<ITestMessageSender>(ServiceKeys.Email);
        await Assert.That(ReferenceEquals(resolved, imposter.Instance())).IsTrue();
    }

    [Test]
    public async Task GetRequiredService_Throws_ForUnregisteredInterface()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        await Assert.That(() => provider.GetRequiredService<ITestExternalService>()).ThrowsException();
        await Assert.That(() => provider.GetRequiredKeyedService<ITestMessageSender>("no-such-key")).ThrowsException();
    }

    [Test]
    public async Task GetService_ReturnsNull_ForUnregisteredAndInvalidKey()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetService<ITestExternalService>()).IsNull();
        await Assert.That(provider.GetKeyedService<ITestMessageSender>("invalid")).IsNull();
        await Assert.That(provider.GetKeyedService<ITestMessageSender>("")).IsNull();
    }

    [Test]
    public async Task Imposter_Verify_WithArgMatchers_WorksCorrectly()
    {
        // Demonstrates Imposter Arg.Any, Arg.Is, Count.Exactly, Count.AtLeast etc. (migration showcase)
        var imposter = ITestMessageSender.Imposter();

        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.Email, imposter.Instance());
        services.AddSingleton<Func<string, ITestMessageSender?>>(sp => k => sp.GetKeyedService<ITestMessageSender>(k));
        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        orchestrator.Send(ServiceKeys.Email, "first");
        orchestrator.Send(ServiceKeys.Email, "second");
        orchestrator.Send(ServiceKeys.Email, "with-predicate");

        // Verification with various matchers
        imposter.Send(Arg<string>.Any()).Called(Count.Exactly(3));
        imposter.Send(Arg<string>.Is(s => s.StartsWith("first"))).Called(Count.Once());
        imposter.Send(Arg<string>.Is(s => s.Contains("predicate"))).Called(Count.Once());
        imposter.Send("first").Called(Count.Once());
        imposter.Send("missing").Called(Count.Never());
        imposter.Send(Arg<string>.Any()).Called(Count.AtLeast(2));
        imposter.Send(Arg<string>.Any()).Called(Count.AtMost(5));
    }
}
