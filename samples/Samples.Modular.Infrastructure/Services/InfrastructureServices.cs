using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Samples.Modular.Infrastructure.Services;

[Autoredi(ServiceLifetime.Singleton, group: "Firebase", priority: 100)]
public class FirebaseCore
{
    public string Name => "Infrastructure Core Firebase";
}

[Autoredi(ServiceLifetime.Scoped, group: "Storage")]
public class DatabaseService
{
    public string Status => "Connected";
}
