namespace Autoredi.Generators.Models;

/// <summary>
/// Mirrors Microsoft.Extensions.DependencyInjection.ServiceLifetime to avoid external dependencies.
/// </summary>
public enum ServiceLifetime
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}

/// <summary>
/// Represents a service registration extracted from the syntax tree.
/// </summary>
public sealed record ServiceRegistration(
    string ImplementationType,
    string? InterfaceType,
    ServiceLifetime Lifetime,
    string? ServiceKey,
    string? Group,
    int Priority,
    string Namespace,
    string AssemblyName
);
