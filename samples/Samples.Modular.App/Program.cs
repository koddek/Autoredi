using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Modular.App.Autoredi;
using Samples.Modular.Infrastructure.Services;

namespace Samples.Modular.App;

[Autoredi(ServiceLifetime.Singleton, group: "Firebase", priority: 1)]
public class LocalFirebaseConfig
{
    public string Name => "Local App Firebase";
}

[Autoredi(ServiceLifetime.Transient)]
public class AppService
{
    public string Message => "Hello from Modular App!";
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Modular Sample ===\n");

        var services = new ServiceCollection();

        // Selective registration of the "Firebase" group.
        Console.WriteLine("Registering 'Firebase' group...");
        services.AddAutorediServicesFirebase();

        // Verify registrations in IServiceCollection
        Console.WriteLine("\nChecking IServiceCollection for Firebase registrations:");
        var registrations = services.ToList();
        
        var firebaseCore = registrations.FirstOrDefault(r => r.ImplementationType == typeof(FirebaseCore));
        var localConfig = registrations.FirstOrDefault(r => r.ImplementationType == typeof(LocalFirebaseConfig));

        Console.WriteLine($" - FirebaseCore registered? {firebaseCore != null}");
        Console.WriteLine($" - LocalFirebaseConfig registered? {localConfig != null}");

        if (firebaseCore != null && localConfig != null)
        {
            var coreIndex = registrations.IndexOf(firebaseCore);
            var localIndex = registrations.IndexOf(localConfig);
            Console.WriteLine($"\nRegistration Order (within Firebase group):");
            Console.WriteLine($" 1. {firebaseCore.ImplementationType?.Name} (Index: {coreIndex}, Priority: 100)");
            Console.WriteLine($" 2. {localConfig.ImplementationType?.Name} (Index: {localIndex}, Priority: 1)");
            
            if (coreIndex < localIndex)
                Console.WriteLine(" >> SUCCESS: High priority service registered first!");
            else
                Console.WriteLine(" >> FAILURE: Priority ordering not respected.");
        }

        // Verify that "Storage" group from Infrastructure is NOT registered yet
        var storageReg = registrations.FirstOrDefault(r => r.ImplementationType == typeof(DatabaseService));
        Console.WriteLine($"\nDatabaseService (Storage group) registered? {storageReg != null}");

        // Register default/ungrouped services for this assembly
        Console.WriteLine("\nRegistering App default services (current assembly only)...");
        services.AddAutorediServices();
        
        var allRegs = services.ToList();
        storageReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(DatabaseService));
        var appReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(AppService));

        Console.WriteLine($" - DatabaseService registered? {storageReg != null}");
        Console.WriteLine($" - AppService registered? {appReg != null}");

        Console.WriteLine("\nRegistering ALL services from this app assembly...");
        services.AddAutorediServicesSamplesModularApp();

        allRegs = services.ToList();
        storageReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(DatabaseService));
        appReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(AppService));

        Console.WriteLine($" - DatabaseService registered? {storageReg != null}");
        Console.WriteLine($" - AppService registered? {appReg != null}");

        // Aggregate registrations across referenced assemblies
        Console.WriteLine("\nRegistering ALL services across modules...");
        services.AddAutorediServicesAll();

        allRegs = services.ToList();
        storageReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(DatabaseService));
        appReg = allRegs.FirstOrDefault(r => r.ImplementationType == typeof(AppService));

        Console.WriteLine($" - DatabaseService registered? {storageReg != null}");
        Console.WriteLine($" - AppService registered? {appReg != null}");

        Console.WriteLine("\n=== Modular Sample Complete ===");
    }
}
