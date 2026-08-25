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
/// Immutable, value-comparable wrapper over <see cref="ImmutableArray{T}"/> so models survive
/// the incremental pipeline cache without reference-equality false misses.
/// </summary>
public readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    public ImmutableArray<T> Values => values;

    public bool IsEmpty => values.IsEmpty;
    public int Count => values.Length;

    public T this[int index] => values[index];

    public bool Equals(EquatableArray<T> other) => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var item in values)
        {
            hash = unchecked((hash * 31) + item.GetHashCode());
        }

        return hash;
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => values.GetEnumerator();
}

/// <summary>
/// A diagnostic payload produced during extraction, transported as plain data so it stays
/// equatable through the incremental cache. Rendered into a real Diagnostic at output time.
/// </summary>
public readonly record struct DiagnosticInfo(string Id, string Arg0, string Arg1 = "") : IEquatable<DiagnosticInfo>;

/// <summary>
/// One class decorated with [Autoredi]: the registrations it expands to plus any
/// extract-time validation issues. InterfaceTypes empty means register-the-class-as-itself.
/// </summary>
public sealed record AutorediTarget(
    EquatableArray<string> InterfaceTypes,
    EquatableArray<DiagnosticInfo> Diagnostics,
    string ImplementationType,
    ServiceLifetime Lifetime,
    string? ServiceKey,
    string? Group,
    int Priority,
    string Namespace,
    string AssemblyName
) : IEquatable<AutorediTarget>
{
    public bool IsValid => Diagnostics.IsEmpty;

    public bool Equals(AutorediTarget? other) =>
        other is not null &&
        ImplementationType == other.ImplementationType &&
        InterfaceTypes.Equals(other.InterfaceTypes) &&
        Lifetime == other.Lifetime &&
        ServiceKey == other.ServiceKey &&
        Group == other.Group &&
        Priority == other.Priority &&
        Namespace == other.Namespace &&
        AssemblyName == other.AssemblyName &&
        Diagnostics.Equals(other.Diagnostics);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = ImplementationType.GetHashCode();
            hash = (hash * 31) + InterfaceTypes.GetHashCode();
            hash = (hash * 31) + (int)Lifetime;
            hash = (hash * 31) + (ServiceKey?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Group?.GetHashCode() ?? 0);
            hash = (hash * 31) + Priority;
            hash = (hash * 31) + Namespace.GetHashCode();
            hash = (hash * 31) + AssemblyName.GetHashCode();
            hash = (hash * 31) + Diagnostics.GetHashCode();
            return hash;
        }
    }
}
