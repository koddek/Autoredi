namespace Autoredi.Generators.Extraction;

/// <summary>
/// Extracts an <see cref="AutorediTarget"/> from each class decorated with [Autoredi],
/// validating the attribute arguments against the target symbol.
/// </summary>
internal static class ServiceRegistrationExtractor
{
    private const ServiceLifetime DefaultLifetime = ServiceLifetime.Transient;

    public static AutorediTarget? Extract(AttributeContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol symbol || context.Attribute is not { } attribute)
        {
            return null;
        }

        var diagnostics = new List<DiagnosticInfo>();
        ValidateImplementationType(symbol, attribute, diagnostics);

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

        diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidLifetime.Id, symbol.Name, attribute));
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
                CollectInterface(attribute, item, symbol, diagnostics, types);
            }

            return DistinctTypes(types);
        }

        var single = GetArgument(attribute, 1, "InterfaceType");
        // A padded default for the omitted parameter surfaces as a null Value; only an
        // explicitly provided typeof(...) reaches interface validation.
        if (single is { } constant && constant.Value is not null)
        {
            var types = new List<INamedTypeSymbol>();
            CollectInterface(attribute, constant, symbol, diagnostics, types);
            return DistinctTypes(types);
        }

        return Array.Empty<INamedTypeSymbol>();
    }

    private static void CollectInterface(
        AttributeData attribute,
        TypedConstant constant,
        INamedTypeSymbol implementor,
        List<DiagnosticInfo> diagnostics,
        List<INamedTypeSymbol> results)
    {
        if (constant.Value is not INamedTypeSymbol type)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, "<null>", attribute));
            return;
        }

        if (type.TypeKind != TypeKind.Interface)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, type.ToDisplayString(), attribute));
            return;
        }

        if (!implementor.AllInterfaces.Contains(type, SymbolEqualityComparer.Default))
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InterfaceNotImplemented.Id, implementor.Name, type.ToDisplayString(), attribute));
            return;
        }

        results.Add(type);
    }

    private static DiagnosticInfo CreateDiagnosticInfo(
        string id,
        string arg0,
        AttributeData attribute) => CreateDiagnosticInfo(id, arg0, string.Empty, attribute);

    private static DiagnosticInfo CreateDiagnosticInfo(
        string id,
        string arg0,
        string arg1,
        AttributeData attribute)
    {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        var filePath = location?.SourceTree?.FilePath;
        return string.IsNullOrEmpty(filePath)
            ? new DiagnosticInfo(id, arg0, arg1)
            : new DiagnosticInfo(
                id,
                arg0,
                arg1,
                filePath,
                location!.SourceSpan,
                location.GetLineSpan().Span);
    }

    private static void ValidateImplementationType(
        INamedTypeSymbol symbol,
        AttributeData attribute,
        List<DiagnosticInfo> diagnostics)
    {
        var hasPublicConstructor = symbol.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public);
        if (symbol.TypeKind == TypeKind.Class
            && !symbol.IsStatic
            && !symbol.IsAbstract
            && !symbol.IsGenericType
            && !symbol.IsFileLocal
            && IsAccessibleFromGeneratedCode(symbol)
            && hasPublicConstructor)
        {
            return;
        }

        diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidImplementationType.Id, symbol.Name, attribute));
    }

    private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is Accessibility.Private
                or Accessibility.Protected
                or Accessibility.ProtectedOrInternal
                or Accessibility.ProtectedAndInternal
                or Accessibility.ProtectedOrFriend
                or Accessibility.ProtectedAndFriend)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<INamedTypeSymbol> DistinctTypes(IEnumerable<INamedTypeSymbol> types) =>
        types
            .GroupBy(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

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
