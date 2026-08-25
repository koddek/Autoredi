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
            .ForAttributeWithMetadataName<AutorediTarget>(Names.AutorediAttFullName)
            .Select(ServiceRegistrationExtractor.Extract)
            .Collect()
            .EmitAll((spc, targets) =>
            {
                if (targets.IsDefaultOrEmpty)
                {
                    return;
                }

                var present = targets.Where(t => t is not null).ToImmutableArray();
                if (present.IsEmpty)
                {
                    return;
                }

                // Extract-time validation issues (invalid lifetime/interface arguments).
                foreach (var target in present)
                {
                    foreach (var info in target.Diagnostics.Values)
                    {
                        spc.ReportDiagnostic(CreateDiagnostic(info));
                    }
                }

                // Emit even when every target is invalid so consumer calls to the generated
                // extension methods still compile; the diagnostics above explain the gaps.
                var assemblyName = present[0].AssemblyName;
                var targetNamespace = assemblyName + ".Autoredi";
                var result = AutorediSourceBuilder.Generate(present, targetNamespace, assemblyName);

                // Output-stage issues (group-name warnings, method-name collisions).
                foreach (var diagnostic in result.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                spc.AddSource("AutorediServices.g.cs", result.Source);
            })
            .Build()
            .Initialize(context);
    }

    private static Diagnostic CreateDiagnostic(DiagnosticInfo info) =>
        string.IsNullOrEmpty(info.Arg1)
            ? Diagnostic.Create(ResolveDescriptor(info.Id), Location.None, info.Arg0)
            : Diagnostic.Create(ResolveDescriptor(info.Id), Location.None, info.Arg0, info.Arg1);

    private static DiagnosticDescriptor ResolveDescriptor(string id) => id switch
    {
        "AUTOREDI007" => Diagnostics.InterfaceNotImplemented,
        "AUTOREDI010" => Diagnostics.InvalidLifetime,
        "AUTOREDI011" => Diagnostics.InvalidInterfaceType,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Autoredi diagnostic id.")
    };
}

internal static class AutorediAllServicesSource
{
    private const string AggregatorMarkerSuffix = ".Autoredi.AutorediServiceCollectionExtensions";

    public static string? TryCreate(Compilation compilation)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(Names.AutorediAttFullName);
        if (attributeSymbol is null)
        {
            return null;
        }

        if (compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary ||
            compilation.Options.OutputKind == OutputKind.NetModule)
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

    /// <summary>
    /// Collects assemblies that contribute Autoredi registrations:
    /// - the current assembly via a bounded walk of its own types (its generated marker type
    ///   is not yet visible during the first generation run),
    /// - referenced assemblies via an O(1) probe of their generated extension class,
    ///   which exists exactly when their generator run produced registrations.
    /// </summary>
    private static List<string> GetAssembliesWithAutoredi(Compilation compilation, INamedTypeSymbol attributeSymbol)
    {
        var assemblies = new List<string>();
        var currentAssemblyName = compilation.Assembly.Name;

        if (NamespaceHasAutoredi(compilation.Assembly.GlobalNamespace, attributeSymbol))
        {
            assemblies.Add(currentAssemblyName);
        }

        foreach (var referenced in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (referenced.GetTypeByMetadataName(referenced.Name + AggregatorMarkerSuffix) is not null)
            {
                assemblies.Add(referenced.Name);
            }
        }

        if (assemblies.Count == 0)
        {
            return assemblies;
        }

        var ordered = new List<string>();
        if (assemblies.Contains(currentAssemblyName))
        {
            ordered.Add(currentAssemblyName);
        }

        ordered.AddRange(assemblies
            .Where(name => name != currentAssemblyName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal));

        return ordered;
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
        foreach (var attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
            {
                return true;
            }
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
}
