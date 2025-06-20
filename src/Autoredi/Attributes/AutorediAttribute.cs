using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Attributes;

/// <summary>
/// Autoredi (Auto Register Dependency Injection), marks a class for automatic registration in the Microsoft Dependency Injection container.
/// </summary>
/// <param name="lifetime">The service lifetime (e.g., Transient, Scoped, Singleton).</param>
/// <param name="interfaceType">The optional interface to register the class against.</param>
/// <param name="serviceKey">The optional key for named registrations when multiple implementations of the same interface exist.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutorediAttribute(
    ServiceLifetime lifetime = ServiceLifetime.Transient,
    Type? interfaceType = null,
    string? serviceKey = null
) : Attribute
{
    /// <summary>
    /// Gets the service lifetime for the registration.
    /// </summary>
    public ServiceLifetime Lifetime => lifetime;

    /// <summary>
    /// Gets the interface name to register the class against, if specified.
    /// </summary>
    public Type? InterfaceType => interfaceType;

    /// <summary>
    /// Gets the service key for named registrations, if specified.
    /// </summary>
    public string? ServiceKey => serviceKey;
}
