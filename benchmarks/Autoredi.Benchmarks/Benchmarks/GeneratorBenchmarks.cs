using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Benchmarks.Benchmarks;

/// <summary>
/// Cold-run cost of the Autoredi generator itself: building the in-memory compilation is
/// excluded; each measured call re-runs every pipeline from a fresh driver. 200 attributed
/// classes gives the benchmark enough work to separate pipeline selection from overhead.
/// </summary>
[MemoryDiagnoser]
public class GeneratorBenchmarks
{
    private readonly string _attributeSource = AttributeSource;
    private CSharpCompilation _compilation = null!;

    private static string AttributeSource => """
        using System.AttributeUsageAttribute;

        namespace Autoredi.Attributes
        {
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class AutorediAttribute : System.Attribute
            {
                public AutorediAttribute(
                    Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime =
                        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
                    System.Type? interfaceType = null,
                    string? serviceKey = null,
                    string? group = null,
                    int priority = 0)
                {
                    Lifetime = lifetime;
                    InterfaceType = interfaceType;
                    ServiceKey = serviceKey;
                    Group = group;
                    Priority = priority;
                }

                public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; }
                public System.Type? InterfaceType { get; }
                public string? ServiceKey { get; }
                public string? Group { get; }
                public int Priority { get; }
                public System.Type[]? InterfaceTypes { get; set; }
            }
        }

        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime
            {
                Singleton = 0,
                Scoped = 1,
                Transient = 2,
            }
        }
        """;

    private static string UserSourceClasses => BuildUserSource();

    private static string BuildUserSource()
    {
        var builder = new StringBuilder(capacity: 200 * 128);
        builder.AppendLine("namespace Bench;");
        builder.AppendLine("public interface IMarker { }");
        for (var i = 0; i < 200; i++)
        {
            builder.Append("[Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IMarker))] public class Service");
            builder.Append(i);
            builder.AppendLine(" : IMarker { }");
        }

        return builder.ToString();
    }

    [GlobalSetup]
    public void Setup()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.ComponentModel.dll")),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ServiceCollection).Assembly.Location),
        };

        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(_attributeSource, path: "AttributeAndEnum.cs"),
            CSharpSyntaxTree.ParseText(UserSourceClasses, path: "UserSource.cs"),
        };

        _compilation = CSharpCompilation.Create(
            "AutorediGeneratorBench",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Benchmark]
    public GeneratorDriver ColdRun() =>
        CSharpGeneratorDriver.Create(new global::Autoredi.Generators.AutorediGenerator())
            .RunGenerators(_compilation);
}
