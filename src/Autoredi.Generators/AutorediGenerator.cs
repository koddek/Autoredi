namespace Autoredi.Generators;

/// <summary>
/// Incremental source generator for Autoredi.
/// </summary>
[Generator]
public sealed class AutorediGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.CompilationProvider.Select((compilation, _) => AutorediAllServicesSource.TryCreate(compilation)),
            (spc, source) =>
            {
                if (source is null || source.Length == 0)
                {
                    return;
                }

                spc.AddSource("AutorediServices.All.g.cs", source);
            });

        context.Flow()
            .ForAttributeWithMetadataName<ServiceRegistration>(Names.AutorediAttFullName)
            .Select(ServiceRegistrationExtractor.Extract)
            .Collect()
            .EmitAll((spc, registrations) =>
            {
                if (registrations.IsDefaultOrEmpty)
                {
                    return;
                }

                // The consumer projects follow [AssemblyName].Autoredi pattern.
                var targetNamespace = registrations[0].AssemblyName + ".Autoredi";

                var source = AutorediSourceBuilder.Generate(registrations, targetNamespace, registrations[0].AssemblyName);
                spc.AddSource("AutorediServices.g.cs", source);
            })
            .Build()
            .Initialize(context);
    }
}

internal static class AutorediAllServicesSource
{
    public static string? TryCreate(Compilation compilation)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(Names.AutorediAttFullName);
        if (attributeSymbol is null)
        {
            return null;
        }

        var assemblyNames = GetAssembliesWithAutoredi(compilation, attributeSymbol);
        if (assemblyNames.Count == 0)
        {
            return null;
        }

        var targetNamespace = compilation.AssemblyName + ".Autoredi";
        return AutorediAllServicesBuilder.Generate(targetNamespace, compilation.AssemblyName ?? compilation.Assembly.Name, assemblyNames);
    }

    private static List<string> GetAssembliesWithAutoredi(Compilation compilation, INamedTypeSymbol attributeSymbol)
    {
        var assemblies = new List<string>();
        if (AssemblyHasAutoredi(compilation.Assembly, attributeSymbol))
        {
            assemblies.Add(compilation.Assembly.Name);
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (AssemblyHasAutoredi(assembly, attributeSymbol))
            {
                assemblies.Add(assembly.Name);
            }
        }

        if (assemblies.Count == 0)
        {
            return assemblies;
        }

        var currentAssembly = compilation.Assembly.Name;
        var ordered = new List<string>();
        if (assemblies.Contains(currentAssembly))
        {
            ordered.Add(currentAssembly);
        }

        ordered.AddRange(assemblies
            .Where(name => name != currentAssembly)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal));

        return ordered;
    }

    private static bool AssemblyHasAutoredi(IAssemblySymbol assembly, INamedTypeSymbol attributeSymbol)
    {
        return NamespaceHasAutoredi(assembly.GlobalNamespace, attributeSymbol);
    }

    private static bool NamespaceHasAutoredi(INamespaceSymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var type in symbol.GetTypeMembers())
        {
            if (TypeHasAutoredi(type, attributeSymbol))
            {
                return true;
            }
        }

        foreach (var ns in symbol.GetNamespaceMembers())
        {
            if (NamespaceHasAutoredi(ns, attributeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TypeHasAutoredi(INamedTypeSymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        if (HasAutorediAttribute(symbol, attributeSymbol))
        {
            return true;
        }

        foreach (var nested in symbol.GetTypeMembers())
        {
            if (TypeHasAutoredi(nested, attributeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAutorediAttribute(INamedTypeSymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(attributeClass, attributeSymbol))
            {
                return true;
            }

            if (attributeClass.ToDisplayString() == Names.AutorediAttFullName)
            {
                return true;
            }
        }

        return false;
    }
}
