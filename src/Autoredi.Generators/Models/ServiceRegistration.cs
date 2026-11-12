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
/// One requested service type. <see cref="IsOpenGeneric"/> marks an open generic definition
/// (<c>typeof(IRepository&lt;&gt;))</c>: MEDI binds the implementation by positional type-parameter
/// substitution, so these entries cannot use the closed-generic <c>ServiceDescriptor</c> path.
/// </summary>
public readonly record struct ServiceTypeEntry(string Display, bool IsOpenGeneric);

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
/// Plain-data location of an attribute in source, transported as plain data so it stays
/// equatable through the incremental cache (a real <see cref="Location"/> is not).
/// </summary>
public readonly record struct SourceLocation(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);
}

/// <summary>
/// A diagnostic payload produced during extraction, transported as plain data so it stays
/// equatable through the incremental cache. Rendered into a real Diagnostic at output time.
/// </summary>
public readonly record struct DiagnosticInfo(
    string Id,
    string Arg0,
    string Arg1 = "",
    string? FilePath = null,
    TextSpan TextSpan = default,
    LinePositionSpan LineSpan = default)
{
    public bool HasLocation => !string.IsNullOrEmpty(FilePath);

    public Location ToLocation() => HasLocation
        ? Location.Create(FilePath!, TextSpan, LineSpan)
        : Location.None;
}

/// <summary>
/// One class decorated with [Autoredi]: the registrations it expands to plus any
/// extract-time validation issues. InterfaceTypes empty means register-the-class-as-itself.
/// Equality is member-wise (InterfaceTypes/Diagnostics are equatable value wrappers), which is
/// what the incremental pipeline caches on.
/// </summary>
public sealed record AutorediTarget(
    EquatableArray<ServiceTypeEntry> InterfaceTypes,
    EquatableArray<DiagnosticInfo> Diagnostics,
    string ImplementationType,
    ServiceLifetime Lifetime,
    string? ServiceKey,
    string? Group,
    int Priority,
    string AssemblyName,
    SourceLocation? Location = null
)
{
    public bool IsValid => Diagnostics.IsEmpty;
}
