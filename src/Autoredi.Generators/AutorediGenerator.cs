namespace Autoredi.Generators;

/// <summary>
/// Incremental source generator for Autoredi.
/// </summary>
[Generator]
public sealed class AutorediGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The per-assembly extension methods carry the registrations. This pipeline derives
        // everything the cross-assembly aggregator needs, cheaply and incrementally:
        // - an attribute scan that only inspects type declarations (Autoredi targets a class,
        //   so every other syntax node is skipped without a semantic lookup);
        // - a compilation-level probe of the referenced assembly symbols, which for typical
        //   SDK builds already sees the full transitive closure of package and project
        //   references (the marker probe itself is O(1) per assembly).
        // Both outputs derive from the same provider instance so Roslyn caches the transform once.
        var targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Names.AutorediAttFullName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, ct) => ServiceRegistrationExtractor.Extract(attributeContext, ct))
            .Where(static target => target is not null)
            .WithTrackingName("Autoredi.AttributeTargets")
            .Collect();

        var referenceProbe = context.CompilationProvider
            .Select(static (compilation, ct) => AutorediAllServicesSource.Probe(compilation, ct))
            .WithTrackingName("Autoredi.ReferencedAssemblyProbe");

        var selfContribution = targets.Select(static (items, _) => !items.IsEmpty);

        context.RegisterSourceOutput(targets, EmitRegistrations);
        context.RegisterSourceOutput(
            selfContribution.Combine(referenceProbe),
            static (spc, pair) => AutorediAllServicesSource.Emit(spc, pair.Left, pair.Right));
    }

    private static void EmitRegistrations(SourceProductionContext spc, ImmutableArray<AutorediTarget?> targets)
    {
        if (targets.IsDefaultOrEmpty)
        {
            return;
        }

        var present = targets.OfType<AutorediTarget>().ToImmutableArray();
        if (present.IsEmpty)
        {
            return;
        }

        // Extract-time validation issues (invalid lifetime/interface arguments).
        foreach (var target in present)
        {
            foreach (var info in target.Diagnostics.Values)
            {
                spc.ReportDiagnostic(ToDiagnostic(info));
            }
        }

        // Emit even when every target is invalid so consumer calls to the generated
        // extension methods still compile; the diagnostics above explain the gaps.
        var assemblyName = present[0].AssemblyName;
        var targetNamespace = AutorediNaming.ToNamespace(assemblyName) + ".Autoredi";
        var result = AutorediSourceBuilder.Generate(present, targetNamespace, assemblyName);

        // Output-stage issues (group-name warnings, method-name collisions).
        foreach (var diagnostic in result.Diagnostics)
        {
            spc.ReportDiagnostic(diagnostic);
        }

        spc.AddSource("AutorediServices.g.cs", result.Source);
    }

    private static Diagnostic ToDiagnostic(DiagnosticInfo info)
    {
        var descriptor = Diagnostics.Resolve(info.Id);
        var location = info.ToLocation();
        return string.IsNullOrEmpty(info.Arg1)
            ? Diagnostic.Create(descriptor, location, info.Arg0)
            : Diagnostic.Create(descriptor, location, info.Arg0, info.Arg1);
    }
}

/// <summary>
/// Plain-data probe of the compilation used to decide what the cross-assembly aggregator
/// emits. Holds no symbols, so it can flow through the incremental cache safely.
/// </summary>
internal readonly struct AggregatorProbe
{
    public AggregatorProbe(
        bool isExecutable,
        string assemblyName,
        ImmutableArray<string> orderedReferencedContributors,
        ImmutableArray<(string AssemblyName, string Suffix)> incompatibleAssemblies)
    {
        IsExecutable = isExecutable;
        AssemblyName = assemblyName;
        OrderedReferencedContributors = orderedReferencedContributors;
        IncompatibleAssemblies = incompatibleAssemblies;
    }

    public bool IsExecutable { get; }

    public string AssemblyName { get; }

    /// <summary>Referenced contributing assemblies, ordinal by name. The calling assembly is appended last.</summary>
    public ImmutableArray<string> OrderedReferencedContributors { get; }

    /// <summary>Assemblies with a marker type but no compatible registration method; skipped with AUTOREDI025.</summary>
    public ImmutableArray<(string AssemblyName, string Suffix)> IncompatibleAssemblies { get; }
}

internal static class AutorediAllServicesSource
{
    private const string AggregatorMarkerSuffix = ".Autoredi.AutorediServiceCollectionExtensions";

    public static AggregatorProbe Probe(Compilation compilation, CancellationToken ct)
    {
        var assemblyName = compilation.AssemblyName ?? compilation.Assembly.Name;
        var isExecutable = compilation.Options.OutputKind is not (OutputKind.DynamicallyLinkedLibrary or OutputKind.NetModule);

        var contributors = new List<string>();
        var incompatible = new List<(string AssemblyName, string Suffix)>();

        foreach (var referenced in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ct.ThrowIfCancellationRequested();
            ProbeReferencedAssembly(referenced, contributors, incompatible, ct);
        }

        // Metadata does not record dependency direction (unused references are elided),
        // so inter-library order cannot be derived from the module graph. Ordinal is the
        // documented, deterministic order; the calling assembly is appended last, which
        // is the part that matters for single-service resolution.
        var ordered = contributors
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToImmutableArray();

        return new AggregatorProbe(isExecutable, assemblyName, ordered, incompatible.ToImmutableArray());
    }

    public static void Emit(SourceProductionContext spc, bool selfContributes, AggregatorProbe probe)
    {
        foreach (var (name, suffix) in probe.IncompatibleAssemblies)
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ReferencedAssemblyNotCompatible, Location.None, name, suffix));
        }

        if (!probe.IsExecutable)
        {
            return;
        }

        List<string> assemblyNames = [.. probe.OrderedReferencedContributors];
        if (selfContributes)
        {
            // The caller is registered last so its registrations win single-service resolution.
            assemblyNames.Add(probe.AssemblyName);
        }

        if (assemblyNames.Count == 0)
        {
            return;
        }

        var targetNamespace = AutorediNaming.ToNamespace(probe.AssemblyName) + ".Autoredi";
        spc.AddSource("AutorediServices.All.g.cs", AutorediAllServicesBuilder.Generate(targetNamespace, assemblyNames));
    }

    /// <summary>
    /// Probes a referenced assembly for Autoredi generated code. For typical .NET SDK builds
    /// the compilation already carries the full transitive closure, so assemblies referenced
    /// only by other referenced assemblies are found here too. The marker type only exists
    /// when that assembly's generator run produced registrations, so the probe is O(1) per
    /// assembly. The registration method is verified, not just the marker type, so an
    /// unrelated type with a colliding name cannot hijack the aggregator.
    /// </summary>
    private static void ProbeReferencedAssembly(
        IAssemblySymbol referenced,
        List<string> contributors,
        List<(string AssemblyName, string Suffix)> incompatible,
        CancellationToken ct)
    {
        var name = referenced.Name;
        var marker = referenced.GetTypeByMetadataName(AutorediNaming.ToNamespace(name) + AggregatorMarkerSuffix);
        if (marker is null)
        {
            return;
        }

        var suffix = AutorediNaming.ToAssemblyMethodSuffix(name);
        if (HasRegistrationMethod(marker, "AddAutorediServices" + suffix))
        {
            contributors.Add(name);
            return;
        }

        incompatible.Add((name, suffix));
    }

    private static bool HasRegistrationMethod(INamedTypeSymbol marker, string methodName)
    {
        foreach (var member in marker.GetMembers(methodName))
        {
            if (member is IMethodSymbol method
                && method.IsStatic
                && method.DeclaredAccessibility == Accessibility.Public
                && method.Parameters.Length == 1
                && method.Parameters[0].Type.MetadataName == "IServiceCollection"
                && method.Parameters[0].Type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                   "global::Microsoft.Extensions.DependencyInjection")
            {
                return true;
            }
        }

        return false;
    }
}
