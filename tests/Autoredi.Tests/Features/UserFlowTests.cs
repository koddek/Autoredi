namespace Autoredi.Tests.Features;

public class UserFlowTests
{
    // --- UserFlow: Simple concrete service (no interface) ---

    [Test]
    public async Task UserFlow_SimpleConcreteService_CanBeResolvedAndIsSingleton()
    {
        // Mirrors README: [Autoredi(Singleton)] AppConfig without interface
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        var a = provider.GetRequiredService<TestSettings>();
        var b = provider.GetRequiredService<TestSettings>();

        await Assert.That(a.ApplicationName).IsEqualTo("TestApp");
        await Assert.That(ReferenceEquals(a, b)).IsTrue();
    }

    // --- UserFlow: Single interface implementation ---

    [Test]
    public async Task UserFlow_SingleInterfaceImplementation_ResolvesViaInterface()
    {
        // Mirrors README: [Autoredi(Transient, typeof(ILogger))] ConsoleLogger : ILogger
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        var logger = provider.GetRequiredService<ITestLogService>();
        await Assert.That(logger).IsOfType(typeof(TestLogService));

        // Interface is transient, self (TestSettings) remains singleton in same container
        var second = provider.GetRequiredService<ITestLogService>();
        await Assert.That(ReferenceEquals(logger, second)).IsFalse();
    }

    // --- UserFlow: Keyed services (multiple implementations, same interface) ---

    [Test]
    public async Task UserFlow_KeyedServices_EachKeyResolvesCorrectImplementation()
    {
        // Mirrors README: Email vs SMS via string keys
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        var email = provider.GetRequiredKeyedService<ITestMessageSender>(ServiceKeys.Email);
        var sms = provider.GetRequiredKeyedService<ITestMessageSender>(ServiceKeys.SMS);
        var push = provider.GetRequiredKeyedService<ITestMessageSender>(ServiceKeys.Push);

        await Assert.That(email).IsOfType(typeof(TestEmailSender));
        await Assert.That(sms).IsOfType(typeof(TestSmsSender));
        await Assert.That(push).IsOfType(typeof(TestPushSender));
    }

    // --- UserFlow: Controller with [FromKeyedServices] injection ---

    [Test]
    public async Task UserFlow_ControllerWithKeyedInjection_GetsCorrectDependency()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        var controller = provider.GetRequiredService<TestServices.TestMessageController>();

        await Assert.That(controller).IsNotNull();
        await Assert.That(controller.GetSender()).IsOfType(typeof(TestSmsSender));
    }

    [Test]
    public async Task UserFlow_Controller_VerifySend_DelegatesToCorrectSender()
    {
        // Use Imposter to prove controller delegates correctly (userflow: unit test a controller)
        var imposter = ITestMessageSender.Imposter();
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddKeyedSingleton<ITestMessageSender>(ServiceKeys.SMS, imposter.Instance());
        var provider = services.BuildServiceProvider();

        var controller = provider.GetRequiredService<TestServices.TestMessageController>();
        controller.SendMessage("hello world");

        imposter.Send(Arg<string>.Any()).Called(Count.Once());
        imposter.Send("hello world").Called(Count.Once());
        imposter.Send(Arg<string>.Is(s => s.Length > 0)).Called(Count.Once());
    }

    // --- UserFlow: Orchestrator with Func resolver (dynamic selection at runtime) ---

    [Test]
    public async Task UserFlow_Orchestrator_DynamicallySelectsSender_WhenKeyProvided()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddSingleton<Func<string, ITestMessageSender?>>(sp => key => sp.GetKeyedService<ITestMessageSender>(key));
        var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        await Assert.That(orchestrator.TryGetSender(ServiceKeys.Email)).IsOfType(typeof(TestEmailSender));
        await Assert.That(orchestrator.TryGetSender(ServiceKeys.SMS)).IsOfType(typeof(TestSmsSender));
        await Assert.That(orchestrator.TryGetSender("invalid")).IsNull();

        await Assert.That(() => orchestrator.Send("invalid", "msg")).ThrowsExactly<InvalidOperationException>();
    }

    // --- UserFlow: Grouped registration (selective) ---

    [Test]
    public async Task UserFlow_SelectiveGroupRegistration_OnlyRequestedGroupIsAvailable()
    {
        // User has modular services split into Firebase / Account / Default groups
        var firebaseOnly = new ServiceCollection();
        firebaseOnly.AddAutorediServicesFirebase();
        var firebaseProvider = firebaseOnly.BuildServiceProvider();

        await Assert.That(firebaseProvider.GetService<FirebaseConfig>()).IsNotNull();
        await Assert.That(firebaseProvider.GetService<AccountService>()).IsNull();
        await Assert.That(firebaseProvider.GetService<DefaultService>()).IsNull();

        var accountOnly = new ServiceCollection();
        accountOnly.AddAutorediServicesAccount();
        var accountProvider = accountOnly.BuildServiceProvider();

        await Assert.That(accountProvider.GetService<AccountService>()).IsNotNull();
        await Assert.That(accountProvider.GetService<FirebaseConfig>()).IsNull();
    }

    // --- UserFlow: Multi-interface (single class, two contracts) ---

    [Test]
    public async Task UserFlow_MultiInterface_SameImplementationServesBothContracts()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var provider = services.BuildServiceProvider();

        var audit = provider.GetRequiredService<IAuditTrail>();
        var telemetry = provider.GetRequiredService<ITelemetrySink>();

        await Assert.That(audit).IsNotNull();
        await Assert.That(telemetry).IsNotNull();
        await Assert.That(audit).IsOfType(typeof(CompositeAuditor));
        await Assert.That(telemetry).IsOfType(typeof(CompositeAuditor));

        // Both contracts resolve; with Singleton they are separate singleton instances per ServiceType (document behavior)
        await Assert.That(audit.Trail()).IsEqualTo("trail");
        await Assert.That(telemetry.Signal()).IsEqualTo("signal");
    }

    // --- UserFlow: TryAdd protection (manual registration wins) ---

    [Test]
    public async Task UserFlow_ManualRegistration_WinsOverAutoredi_WhenRegisteredFirst()
    {
        // User wants to override a service for testing (e.g., replace FirebaseConfig)
        var imposter = ITestLogService.Imposter();
        // Setup must allow Log call? Use Implicit so void passes without explicit setup

        var services = new ServiceCollection();
        services.AddSingleton<ITestLogService>(imposter.Instance()); // manual first
        services.AddAutorediServices(); // Autoredi must not replace it (TryAdd)
        var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<ITestLogService>();
        await Assert.That(ReferenceEquals(resolved, imposter.Instance())).IsTrue();

        // Prove it is the imposter and still mock-verifiable
        resolved.Log("probe");
        imposter.Log(Arg<string>.Any()).Called(Count.Once());
    }

    // --- UserFlow: Priority ordering is observable in ServiceCollection ---

    [Test]
    public async Task UserFlow_PriorityOrdering_FirebaseGroup_HighPriorityFirst()
    {
        var services = new ServiceCollection();
        services.AddAutorediServicesFirebase();

        var order = services.Select(d => d.ServiceType.Name).ToList();
        var configIdx = order.IndexOf(nameof(FirebaseConfig));
        var repoIdx = order.IndexOf(nameof(FirebaseRepo));
        var loggerIdx = order.IndexOf(nameof(FirebaseLogger));

        await Assert.That(configIdx).IsLessThan(repoIdx);
        await Assert.That(repoIdx).IsLessThan(loggerIdx);
    }
}
