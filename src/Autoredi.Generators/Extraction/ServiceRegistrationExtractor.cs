namespace Autoredi.Generators.Extraction;

/// <summary>
/// Extracts ServiceRegistration models from attributes.
/// </summary>
internal static class ServiceRegistrationExtractor
{
    public static ServiceRegistration Extract(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes.FirstOrDefault();

        if (attribute == null)
        {
            return null!;
        }

        var lifetime = ServiceLifetime.Transient;
        string? interfaceType = null;
        string? serviceKey = null;
        string? group = null;
        var priority = 0;

        // Extract from constructor arguments
        if (attribute.ConstructorArguments.Length > 0)
        {
            lifetime = (ServiceLifetime)(int)attribute.ConstructorArguments[0].Value!;
        }

        if (attribute.ConstructorArguments.Length > 1)
        {
            var interfaceTypeSymbol = attribute.ConstructorArguments[1].Value as INamedTypeSymbol;
            interfaceType = interfaceTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        if (attribute.ConstructorArguments.Length > 2)
        {
            serviceKey = attribute.ConstructorArguments[2].Value?.ToString();
        }

        if (attribute.ConstructorArguments.Length > 3)
        {
            group = attribute.ConstructorArguments[3].Value?.ToString();
        }

        if (attribute.ConstructorArguments.Length > 4)
        {
            priority = (int)attribute.ConstructorArguments[4].Value!;
        }

        // Extract from named arguments (overrides constructor arguments if present)
        foreach (var namedArg in attribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Lifetime":
                    lifetime = (ServiceLifetime)(int)namedArg.Value.Value!;
                    break;
                case "InterfaceType":
                    interfaceType = (namedArg.Value.Value as INamedTypeSymbol)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    break;
                case "ServiceKey":
                    serviceKey = namedArg.Value.Value?.ToString();
                    break;
                case "Group":
                    group = namedArg.Value.Value?.ToString();
                    break;
                case "Priority":
                    priority = (int)namedArg.Value.Value!;
                    break;
            }
        }

        return new ServiceRegistration(
            ImplementationType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            InterfaceType: interfaceType,
            Lifetime: lifetime,
            ServiceKey: serviceKey,
            Group: group,
            Priority: priority,
            Namespace: symbol.ContainingNamespace.ToDisplayString(),
            AssemblyName: symbol.ContainingAssembly.Name
        );
    }
}
