namespace Autoredi.Generators.Extraction;

/// <summary>
/// Extracts an <see cref="AutorediTarget"/> from each class decorated with [Autoredi],
/// validating the attribute arguments against the target symbol.
/// </summary>
internal static class ServiceRegistrationExtractor
{
    private const ServiceLifetime DefaultLifetime = ServiceLifetime.Transient;

    public static AutorediTarget Extract(AttributeContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol symbol || context.Attribute is not { } attribute)
        {
            return null!;
        }

        var diagnostics = new List<DiagnosticInfo>();

        var lifetime = ExtractLifetime(attribute, symbol, diagnostics);
        var serviceKey = ExtractStringArgument(attribute, 2, "ServiceKey");
        var group = ExtractStringArgument(attribute, 3, "Group");
        var priority = ExtractPriority(attribute);

        var interfaceNames = ExtractInterfaceTypes(attribute, symbol, diagnostics)
            .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        return new AutorediTarget(
            InterfaceTypes: new EquatableArray<string>(interfaceNames),
            Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()),
            ImplementationType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime: lifetime,
            ServiceKey: serviceKey,
            Group: group,
            Priority: priority,
            Namespace: symbol.ContainingNamespace.ToDisplayString(),
            AssemblyName: symbol.ContainingAssembly.Name
        );
    }

    private static ServiceLifetime ExtractLifetime(AttributeData attribute, INamedTypeSymbol symbol, List<DiagnosticInfo> diagnostics)
    {
        var raw = GetArgument(attribute, 0, "Lifetime");
        if (raw is null || raw.Value.Value is null)
        {
            return DefaultLifetime;
        }

        if (raw.Value.Value is int value && value >= 0 && value <= (int)ServiceLifetime.Transient)
        {
            return (ServiceLifetime)value;
        }

        diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidLifetime.Id, symbol.Name));
        return DefaultLifetime;
    }

    private static string? ExtractStringArgument(AttributeData attribute, int position, string namedName) =>
        GetArgument(attribute, position, namedName)?.Value?.ToString();

    private static int ExtractPriority(AttributeData attribute) =>
        GetArgument(attribute, 4, "Priority")?.Value as int? ?? 0;

    /// <summary>
    /// Resolves the requested service types. Explicit InterfaceTypes (array) replaces the
    /// single InterfaceType when present with at least one entry; otherwise the single value
    /// applies. No entries at all means register-the-class-as-itself.
    /// </summary>
    private static IReadOnlyList<INamedTypeSymbol> ExtractInterfaceTypes(
        AttributeData attribute,
        INamedTypeSymbol symbol,
        List<DiagnosticInfo> diagnostics)
    {
        // Roslyn pads ConstructorArguments to every constructor parameter, so an omitted
        // InterfaceTypes still appears as an Array constant whose Values is a default
        // ImmutableArray. Treat only non-default, non-empty arrays as explicitly provided.
        var arrayArg = GetArgument(attribute, 5, "InterfaceTypes");
        if (arrayArg is { } array
            && array.Kind == TypedConstantKind.Array
            && !array.Values.IsDefault
            && array.Values.Length > 0)
        {
            var types = new List<INamedTypeSymbol>();
            foreach (var item in array.Values)
            {
                CollectInterface(item, symbol, diagnostics, types);
            }

            return types;
        }

        var single = GetArgument(attribute, 1, "InterfaceType");
        // A padded default for the omitted parameter surfaces as a null Value; only an
        // explicitly provided typeof(...) reaches interface validation.
        if (single is { } constant && constant.Value is not null)
        {
            var types = new List<INamedTypeSymbol>();
            CollectInterface(constant, symbol, diagnostics, types);
            return types;
        }

        return Array.Empty<INamedTypeSymbol>();
    }

    private static void CollectInterface(
        TypedConstant constant,
        INamedTypeSymbol implementor,
        List<DiagnosticInfo> diagnostics,
        List<INamedTypeSymbol> results)
    {
        if (constant.Value is not INamedTypeSymbol type)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, "<null>"));
            return;
        }

        if (type.TypeKind != TypeKind.Interface)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, type.ToDisplayString()));
            return;
        }

        if (!implementor.AllInterfaces.Contains(type, SymbolEqualityComparer.Default))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InterfaceNotImplemented.Id, implementor.Name, type.ToDisplayString()));
            return;
        }

        results.Add(type);
    }

    private static TypedConstant? GetArgument(AttributeData attribute, int position, string namedName)
    {
        var namedArguments = attribute.NamedArguments;
        for (var i = 0; i < namedArguments.Length; i++)
        {
            if (string.Equals(namedArguments[i].Key, namedName, StringComparison.Ordinal))
            {
                return namedArguments[i].Value;
            }
        }

        return attribute.ConstructorArguments.Length > position ? attribute.ConstructorArguments[position] : null;
    }
}
