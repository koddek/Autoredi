using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.Modular.App.Autoredi;
using Samples.Modular.Infrastructure.Services;
// Same generated class name in both assemblies, so alias the referenced one for static calls.
using InfrastructureAutoredi = Samples.Modular.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;

namespace Samples.Modular.App;

// --- App-local services ---

public interface IAudit
{
    string Trail();
}

public interface ITelemetry
{
    string Signal();
}

/// <summary>
/// Demonstrates multi-interface registration: one attribute fans out to
/// one TryAddEnumerable descriptor per interface.
/// </summary>
[Autoredi(ServiceLifetime.Singleton, InterfaceTypes = [typeof(IAudit), typeof(ITelemetry)])]
public class CompositeAuditor : IAudit, ITelemetry
{
    public string Trail() => "audit-ok";
    public string Signal() => "telemetry-ok";
}

[Autoredi(ServiceLifetime.Transient)]
public class AppGreeter
{
    public string Greet() => "hello from Modular.App";
}

public enum Channel
{
    Email,
    Push
}

[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), "app-email", group: "Notify")]
public class AppEmailNotificationService : INotificationService
{
    public string Channel => "Email";
    public void Send(string message) => Console.WriteLine($"  [EMAIL] {message}");
}

public static class Keys
{
    public const string Push = "push";
}

// --- Program ---

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Modular Demo ===");
        Console.WriteLine("Demonstrates cross-assembly aggregation, selective groups,\nmulti-interface registration, and keyed services.\n");

        SelectiveRegistration();

        AllAssembliesRegistration();

        Console.WriteLine("=== Demo Complete ===");
    }

    private static void SelectiveRegistration()
    {
        Console.WriteLine("-- Selective: app defaults + Infrastructure 'Storage' group --");
        using var provider = CreateProvider(services =>
        {
            // App assembly, default (ungrouped) services only.
            services.AddAutorediServices();

            // Referenced assembly, one group only. Same generated class name,
            // different namespace, so call it statically via the alias above.
            InfrastructureAutoredi.AddAutorediServicesStorage(services);
        });

        Report(provider);
    }

    private static void AllAssembliesRegistration()
    {
        Console.WriteLine("\n-- AddAutorediServicesAll: every registration from app + referenced assemblies --");
        using var provider = CreateProvider(services => services.AddAutorediServicesAll());

        Report(provider);

        var notifier = provider.GetKeyedService<INotificationService>("app-email");
        notifier?.Send("keyed resolution across the aggregate");

        // Double-call is safe under TryAdd semantics: same descriptor count as one call.
        var once = new ServiceCollection().AddAutorediServicesAll();
        var twice = new ServiceCollection().AddAutorediServicesAll().AddAutorediServicesAll();
        Console.WriteLine($"\nDouble-call idempotent: {once.Count == twice.Count} ({once.Count} descriptors both times)");
    }

    private static ServiceProvider CreateProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static void Report(ServiceProvider provider)
    {
        Console.WriteLine($"  IAudit      -> {provider.GetService<IAudit>()?.Trail()}");
        Console.WriteLine($"  ITelemetry  -> {provider.GetService<ITelemetry>()?.Signal()}");
        Console.WriteLine($"  AppGreeter  -> {provider.GetService<AppGreeter>()?.Greet()}");
        Console.WriteLine($"  DatabaseService (Storage group) -> {provider.GetService<DatabaseService>()?.Status ?? "NOT REGISTERED"}");
        Console.WriteLine($"  FirebaseCore (excluded here) -> {(provider.GetService<FirebaseCore>() is null ? "not registered" : "registered")}");
    }
}
