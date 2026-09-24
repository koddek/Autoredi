using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Autoredi.Tests.Generator;

/// <summary>
/// Runs the Autoredi generator against hermetic in-memory sources and returns
/// the generated trees plus generator diagnostics. The [Autoredi] attribute and a
/// ServiceLifetime mirror are embedded per compilation, so no external package
/// references beyond BCL facades are needed.
/// </summary>
internal static class GeneratorTestHarness
{
    private const string AttributeAndEnumSource = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime
            {
                Singleton = 0,
                Scoped = 1,
                Transient = 2
            }
        }

        namespace Autoredi.Attributes
        {
            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class AutorediAttribute(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime =
                    Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
                System.Type? interfaceType = null,
                string? serviceKey = null,
                string? group = null,
                int priority = 0
            ) : System.Attribute
            {
                public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime => lifetime;
                public System.Type? InterfaceType => interfaceType;
                public string? ServiceKey => serviceKey;
                public string? Group => group;
                public int Priority => priority;
                public System.Type[]? InterfaceTypes { get; set; }
            }
        }
        """;

    public static (ImmutableArray<Diagnostic> Diagnostics, ImmutableDictionary<string, string> Sources) Run(
        string userSource,
        string assemblyName = "Probe",
        bool generateAggregator = false)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.Extensions.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Reflection.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ServiceCollection).Assembly.Location),
        };

        var syntaxTrees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(AttributeAndEnumSource, path: "AttributeAndEnum.cs"),
            CSharpSyntaxTree.ParseText(userSource, path: "UserSource.cs"),
        };

        if (generateAggregator)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                "public static class AutorediTestEntryPoint { public static void Main() { } }",
                path: "EntryPoint.cs"));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(generateAggregator ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new global::Autoredi.Generators.AutorediGenerator());
        var result = driver.RunGenerators(compilation).GetRunResult();

        var sources = result.GeneratedTrees.ToImmutableDictionary(
            t => Path.GetFileName(t.FilePath),
            t => t.GetText().ToString());

        var outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees);
        var compilationDiagnostics = outputCompilation.GetDiagnostics();
        var diagnostics = result.Diagnostics.AddRange(compilationDiagnostics);

        return (diagnostics, sources);
    }

    public static bool HasDiagnostic(ImmutableArray<Diagnostic> diagnostics, string id) =>
        diagnostics.Any(d => d.Id == id);

    public static bool HasCompilationErrors(ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error
            && d.Id.StartsWith("CS", StringComparison.Ordinal));

    public static bool HasLocatedDiagnostic(ImmutableArray<Diagnostic> diagnostics, string id) =>
        diagnostics.Any(d => d.Id == id && d.Location != Location.None);

    public static bool SourceContains(ImmutableDictionary<string, string> sources, string fragment) =>
        sources.Values.Any(source => source.Contains(fragment, StringComparison.Ordinal));
}
