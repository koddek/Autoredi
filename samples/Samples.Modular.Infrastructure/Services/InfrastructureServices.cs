using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;

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

/// <summary>
/// One implementation of a contract owned by a different assembly. Autoredi must keep
/// this descriptor when the app aggregates every assembly, even though the app
/// contributes its own implementation of the same contract.
/// </summary>
[Autoredi(ServiceLifetime.Transient, typeof(IAuditChannel))]
public class FileAuditChannel : IAuditChannel
{
    public string Name => "file";
}
