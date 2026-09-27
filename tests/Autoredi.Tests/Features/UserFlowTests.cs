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
        using var provider = services.BuildServiceProvider();

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
        using var provider = services.BuildServiceProvider();

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
        using var provider = services.BuildServiceProvider();

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
        using var provider = services.BuildServiceProvider();

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
        using var provider = services.BuildServiceProvider();

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
        using var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<TestServices.TestMessageOrchestrator>();

        await Assert.That(orchestrator.TryGetSender(ServiceKeys.Email)).IsOfType(typeof(TestEmailSender));
        await Assert.That(orchestrator.TryGetSender(ServiceKeys.SMS)).IsOfType(typeof(TestSmsSender));
        await Assert.That(orchestrator.TryGetSender("invalid")).IsNull();

        await Assert.That(() => orchestrator.Send("invalid", "msg")).ThrowsExactly<InvalidOperationException>();
    }

    // --- UserFlow: Grouped registration (selective) ---

    // --- UserFlow: Multi-interface (single class, two contracts) ---

    [Test]
    public async Task UserFlow_MultiInterface_SameImplementationServesBothContracts()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

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
        // A consumer overrides an interface implementation for testing. Registering the
        // instance first must keep it as the resolved service after the generated call.
        var imposter = ITestLogService.Imposter();

        var services = new ServiceCollection();
        services.AddSingleton<ITestLogService>(imposter.Instance()); // manual first
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<ITestLogService>();
        await Assert.That(ReferenceEquals(resolved, imposter.Instance())).IsTrue();

        // Prove it is the imposter and still mock-verifiable
        resolved.Log("probe");
        imposter.Log(Arg<string>.Any()).Called(Count.Once());
    }
}
