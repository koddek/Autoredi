namespace Autoredi.Generators.Extraction;

/// <summary>
/// Extracts an <see cref="AutorediTarget"/> from each class decorated with [Autoredi],
/// validating the attribute arguments against the target symbol.
/// </summary>
internal static class ServiceRegistrationExtractor
{
    private const ServiceLifetime DefaultLifetime = ServiceLifetime.Transient;

    public static AutorediTarget? Extract(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        // ForAttributeWithMetadataName only forwards the attributes that matched the requested name.
        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
        {
            return null;
        }

        var location = ToSourceLocation(attribute, ct);
        var diagnostics = new List<DiagnosticInfo>();

        // Validate the requested service types first; a generic implementation is only
        // legitimate when every requested service type is an open generic definition.
        var (serviceTypes, requestedCount, requestedOpenCount) =
            ExtractInterfaceTypes(attribute, symbol, location, diagnostics, ct);
        var allowsGeneric = requestedCount > 0 && requestedOpenCount == requestedCount;
        ValidateImplementationType(symbol, location, allowsGeneric, diagnostics);

        var lifetime = ExtractLifetime(attribute, symbol, location, diagnostics, ct);
        var serviceKey = ExtractStringArgument(attribute, 2, "ServiceKey");
        var group = ExtractStringArgument(attribute, 3, "Group");
        var priority = ExtractPriority(attribute);

        var interfaceEntries = serviceTypes
            .Select(t => new ServiceTypeEntry(
                t.IsUnboundGenericType ? OpenGenericTypeName(t) : t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                t.IsUnboundGenericType))
            .ToImmutableArray();

        return new AutorediTarget(
            InterfaceTypes: new EquatableArray<ServiceTypeEntry>(interfaceEntries),
            Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()),
            ImplementationType: symbol.IsGenericType
                ? OpenGenericTypeName(symbol)
                : symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime: lifetime,
            ServiceKey: serviceKey,
            Group: group,
            Priority: priority,
            AssemblyName: symbol.ContainingAssembly.Name,
            Location: location
        );
    }

    private static SourceLocation? ToSourceLocation(AttributeData attribute, CancellationToken ct)
    {
        var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(ct);
        if (syntax is null)
        {
            return null;
        }

        var syntaxLocation = syntax.GetLocation();
        var filePath = syntaxLocation.SourceTree?.FilePath;
        return string.IsNullOrEmpty(filePath)
            ? null
            : new SourceLocation(filePath!, syntaxLocation.SourceSpan, syntaxLocation.GetLineSpan().Span);
    }

    private static ServiceLifetime ExtractLifetime(AttributeData attribute, INamedTypeSymbol symbol, SourceLocation? location, List<DiagnosticInfo> diagnostics, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var raw = GetArgument(attribute, 0, "Lifetime");
        if (raw is null || raw.Value.Value is null)
        {
            return DefaultLifetime;
        }

        if (raw.Value.Value is int value && value >= 0 && value <= (int)ServiceLifetime.Transient)
        {
            return (ServiceLifetime)value;
        }

        diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidLifetime.Id, symbol.Name, location));
        return DefaultLifetime;
    }

    private static string? ExtractStringArgument(AttributeData attribute, int position, string namedName) =>
        GetArgument(attribute, position, namedName)?.Value?.ToString();

    private static int ExtractPriority(AttributeData attribute) =>
        GetArgument(attribute, 4, "Priority")?.Value as int? ?? 0;

    /// <summary>
    /// Resolves the requested service types. Explicit InterfaceTypes (array) replaces the
    /// single InterfaceType when present with at least one entry; otherwise the single value
    /// applies. No entries at all means register-the-class-as-itself. Also reports how many
    /// entries were requested and how many of those were open generic definitions, so the
    /// implementation validator can tell a legitimate generic implementation from a broken one.
    /// </summary>
    private static (IReadOnlyList<INamedTypeSymbol> Types, int RequestedCount, int RequestedOpenCount) ExtractInterfaceTypes(
        AttributeData attribute,
        INamedTypeSymbol symbol,
        SourceLocation? location,
        List<DiagnosticInfo> diagnostics,
        CancellationToken ct)
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
            var requestedOpen = 0;
            foreach (var item in array.Values)
            {
                CollectInterface(item, symbol, location, diagnostics, types, ct);
                if (item.Value is INamedTypeSymbol t && t.IsUnboundGenericType)
                {
                    requestedOpen++;
                }
            }

            return (DistinctTypes(types), array.Values.Length, requestedOpen);
        }

        var single = GetArgument(attribute, 1, "InterfaceType");
        // A padded default for the omitted parameter surfaces as a null Value; only an
        // explicitly provided typeof(...) reaches interface validation.
        if (single is { } constant && constant.Value is not null)
        {
            var types = new List<INamedTypeSymbol>();
            CollectInterface(constant, symbol, location, diagnostics, types, ct);
            var requestedOpen = constant.Value is INamedTypeSymbol t && t.IsUnboundGenericType ? 1 : 0;
            return (DistinctTypes(types), 1, requestedOpen);
        }

        return ([], 0, 0);
    }

    private static void CollectInterface(
        TypedConstant constant,
        INamedTypeSymbol implementor,
        SourceLocation? location,
        List<DiagnosticInfo> diagnostics,
        List<INamedTypeSymbol> results,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (constant.Value is not INamedTypeSymbol type)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, "<null>", location));
            return;
        }

        if (type.TypeKind != TypeKind.Interface)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidInterfaceType.Id, implementor.Name, type.ToDisplayString(), location));
            return;
        }

        if (!IsAccessibleFromGeneratedCode(type, implementor.ContainingAssembly))
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.ServiceTypeNotAccessible.Id, implementor.Name, type.ToDisplayString(), location));
            return;
        }

        if (type.IsUnboundGenericType)
        {
            CollectOpenGeneric(type, implementor, location, diagnostics, results, ct);
            return;
        }

        if (!implementor.AllInterfaces.Contains(type, SymbolEqualityComparer.Default))
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InterfaceNotImplemented.Id, implementor.Name, type.ToDisplayString(), location));
            return;
        }

        results.Add(type);
    }

    /// <summary>
    /// Validates an open generic service type such as <c>typeof(IRepository&lt;&gt;)</c>. The
    /// implementation must be a generic class and its type parameters must map positionally to the
    /// service type's parameters; that is how MEDI binds an open generic descriptor.
    /// </summary>
    private static void CollectOpenGeneric(
        INamedTypeSymbol serviceType,
        INamedTypeSymbol implementor,
        SourceLocation? location,
        List<DiagnosticInfo> diagnostics,
        List<INamedTypeSymbol> results,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var serviceKey = DefinitionKey(serviceType);
        var implemented = implementor.AllInterfaces.FirstOrDefault(i => DefinitionKey(i) == serviceKey);
        if (implemented is null)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InterfaceNotImplemented.Id, implementor.Name, serviceType.ToDisplayString(), location));
            return;
        }

        var arity = serviceType.TypeParameters.Length;
        var matches = implementor.TypeParameters.Length == arity
            && implemented.TypeParameters.Length == arity;
        if (matches)
        {
            for (var i = 0; i < arity; i++)
            {
                if (implemented.TypeArguments[i] is not ITypeParameterSymbol parameter
                    || !SymbolEqualityComparer.Default.Equals(parameter, implementor.TypeParameters[i]))
                {
                    matches = false;
                    break;
                }
            }
        }

        if (!matches)
        {
            diagnostics.Add(CreateDiagnosticInfo(Diagnostics.OpenGenericServiceTypeMismatch.Id, implementor.Name, serviceType.ToDisplayString(), location));
            return;
        }

        results.Add(serviceType);
    }

    /// <summary>
    /// Matches an open generic definition to a constructed interface without relying on
    /// unbound-vs-definition symbol identity: metadata name, arity, assembly and containing
    /// namespace are unique per declaration.
    /// </summary>
    private static string DefinitionKey(INamedTypeSymbol symbol)
    {
        var definition = symbol.OriginalDefinition;
        return definition.MetadataName + "|" + definition.TypeParameters.Length + "|"
            + definition.ContainingAssembly.Name + "|"
            + definition.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Formats a type as an unbound generic type reference (<c>global::Ns.Type&lt;&gt;</c>) so the
    /// generated <c>typeof()</c> compiles for open generic service types and implementations.
    /// </summary>
    private static string OpenGenericTypeName(INamedTypeSymbol symbol)
    {
        var definition = symbol.OriginalDefinition;
        var segments = new Stack<string>();
        for (var current = definition; current is not null; current = current.ContainingType)
        {
            var arity = current.TypeParameters.Length;
            segments.Push(arity == 0 ? current.Name : current.Name + "<" + new string(',', arity - 1) + ">");
        }

        var prefix = definition.ContainingNamespace.IsGlobalNamespace
            ? "global::"
            : definition.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ".";
        return prefix + string.Join(".", segments);
    }

    private static DiagnosticInfo CreateDiagnosticInfo(
        string id,
        string arg0,
        SourceLocation? location) => CreateDiagnosticInfo(id, arg0, string.Empty, location);

    private static DiagnosticInfo CreateDiagnosticInfo(
        string id,
        string arg0,
        string arg1,
        SourceLocation? location) =>
        location is { } loc
            ? new DiagnosticInfo(id, arg0, arg1, loc.FilePath, loc.TextSpan, loc.LineSpan)
            : new DiagnosticInfo(id, arg0, arg1);

    private static void ValidateImplementationType(
        INamedTypeSymbol symbol,
        SourceLocation? location,
        bool allowsOpenGeneric,
        List<DiagnosticInfo> diagnostics)
    {
        var hasPublicConstructor = symbol.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public);
        // A generic class is legitimate only when every requested service type is an open generic definition.
        if (symbol.TypeKind == TypeKind.Class
            && !symbol.IsStatic
            && !symbol.IsAbstract
            && (!symbol.IsGenericType || allowsOpenGeneric)
            && !symbol.IsFileLocal
            && IsAccessibleFromGeneratedCode(symbol, symbol.ContainingAssembly)
            && hasPublicConstructor)
        {
            return;
        }

        diagnostics.Add(CreateDiagnosticInfo(Diagnostics.InvalidImplementationType.Id, symbol.Name, location));
    }

    /// <summary>
    /// Whether generated code (emitted into the same assembly as the decorated class, but
    /// outside its containing types) can reference the given type from the given assembly.
    /// File-local types and members are never visible; internal members are only visible
    /// within their own assembly; anything nested inside an invisible type is invisible too.
    /// </summary>
    private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol symbol, IAssemblySymbol codeAssembly)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.IsFileLocal)
            {
                return false;
            }

            var accessibility = current.DeclaredAccessibility;
            var visible = accessibility == Accessibility.Public
                || (accessibility == Accessibility.Internal
                    && SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, codeAssembly));
            if (!visible)
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
