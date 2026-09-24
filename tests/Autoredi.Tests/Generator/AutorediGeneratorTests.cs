namespace Autoredi.Tests.Generator;

/// <summary>
/// Generator-level tests: run the incremental generator directly via
/// CSharpGeneratorDriver and assert on emitted sources and diagnostics.
/// </summary>
public class AutorediGeneratorTests
{
    private const string Main = "AutorediServices.g.cs";

    [Test]
    public async Task SelfRegistration_UsesTryAdd()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
            public class Config { }
            """);

        await Assert.That(GeneratorTestHarness.SourceContains(sources, "services.TryAddSingleton<global::Probe.Config>();")).IsTrue();
        await Assert.That(sources.ContainsKey(Main)).IsTrue();
    }

    [Test]
    public async Task SingleInterface_UsesTryAdd()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IFoo { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IFoo))]
            public class FooImpl : IFoo { }
            """);

        // Single implementation per (serviceType, key) uses TryAdd so manual registrations are preserved
        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "services.TryAdd(ServiceDescriptor.Scoped<global::Probe.IFoo, global::Probe.FooImpl>());")).IsTrue();
    }

    [Test]
    public async Task MultiInterface_FansOutOneDescriptorPerInterface()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IRepo { }
            public interface ICache { }

            [global::Autoredi.Attributes.Autoredi(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
                InterfaceTypes = new[] { typeof(IRepo), typeof(ICache) })]
            public class Store : IRepo, ICache { }
            """);

        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "services.TryAdd(ServiceDescriptor.Singleton<global::Probe.IRepo, global::Probe.Store>());")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "services.TryAdd(ServiceDescriptor.Singleton<global::Probe.ICache, global::Probe.Store>());")).IsTrue();
        // Interfaces-only: no self registration when interfaces are requested.
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "TryAddSingleton<global::Probe.Store>()")).IsFalse();
    }

    [Test]
    public async Task TwoImplementations_SameInterface_UsesTryAddEnumerable()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IRepo { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IRepo))]
            public class RepoA : IRepo { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IRepo))]
            public class RepoB : IRepo { }
            """);

        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "services.TryAddEnumerable(ServiceDescriptor.Transient<global::Probe.IRepo, global::Probe.RepoA>());")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "services.TryAddEnumerable(ServiceDescriptor.Transient<global::Probe.IRepo, global::Probe.RepoB>());")).IsTrue();
    }

    [Test]
    public async Task KeyedService_EmitsKeyedEnumerableWithEscapedKey()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface ISender { }

            [global::Autoredi.Attributes.Autoredi(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
                typeof(ISender),
                "quote\"and\\slash")]
            public class Sender : ISender { }
            """);

        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "ServiceDescriptor.KeyedTransient<global::Probe.ISender, global::Probe.Sender>(\"quote\\\"and\\\\slash\")")).IsTrue();
    }

    [Test]
    public async Task InvalidLifetime_ReportsAutoredi010_AndSkipsEmission()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi((Microsoft.Extensions.DependencyInjection.ServiceLifetime)42)]
            public class Weird { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI010")).IsTrue();
        await Assert.That(GeneratorTestHarness.HasLocatedDiagnostic(diagnostics, "AUTOREDI010")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "global::Probe.Weird")).IsFalse();
    }

    [Test]
    public async Task NonInterfaceServiceType_ReportsAutoredi011()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;

            public class NotAnInterface { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(NotAnInterface))]
            public class Handler { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI011")).IsTrue();
    }

    [Test]
    public async Task UnimplementedInterface_ReportsAutoredi007()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IMissing { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IMissing))]
            public class Broken { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI007")).IsTrue();
    }

    [Test]
    public async Task InvalidGroupName_WarnsAndSanitizesMethod()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "my-group")]
            public class Grouped { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI018")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "AddAutorediServicesMyGroup")).IsTrue();
    }

    [Test]
    public async Task GroupNamedAll_CollidesWithAggregator_ReportsAutoredi023()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "All")]
            public class Clashing { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI023")).IsTrue();
        // The colliding group method is skipped in the per-assembly file instead of
        // emitting a duplicate member; only the aggregator itself keeps that name.
        await Assert.That(sources[Main].Contains("public static IServiceCollection AddAutorediServicesAll(", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Priority_OrdersRegistrationsDescending()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(priority: 10)]
            public class High { }

            [global::Autoredi.Attributes.Autoredi(priority: 100)]
            public class Highest { }

            [global::Autoredi.Attributes.Autoredi()]
            public class Low { }
            """);

        var source = sources[Main];
        var highIndex = source.IndexOf("global::Probe.High>", StringComparison.Ordinal);
        var highestIndex = source.IndexOf("global::Probe.Highest>", StringComparison.Ordinal);
        var lowIndex = source.IndexOf("global::Probe.Low>", StringComparison.Ordinal);

        await Assert.That(highestIndex).IsLessThan(highIndex);
        await Assert.That(highIndex).IsLessThan(lowIndex);
    }

    [Test]
    public async Task AssemblyWideMethod_RegistersEveryGroup_IncludingKeyed()
    {
        var (_, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IChannel { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
            public class Plain { }

            [global::Autoredi.Attributes.Autoredi(group: "Notify")]
            public class Grouped { }
            """);

        var source = sources[Main];
        var assemblyWideIndex = source.IndexOf("AddAutorediServicesProbe", StringComparison.Ordinal);

        await Assert.That(assemblyWideIndex).IsGreaterThan(0);
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "AddAutorediServicesNotify")).IsTrue();
    }

    [Test]
    public async Task InvalidType_ReportsDiagnostic()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi]
            public abstract class AbstractService { }

            [global::Autoredi.Attributes.Autoredi]
            public static class StaticService { }

            [global::Autoredi.Attributes.Autoredi]
            public class GenericService<T> { }

            [global::Autoredi.Attributes.Autoredi]
            public class NoPublicConstructor
            {
                private NoPublicConstructor() { }
            }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI012")).IsTrue();
        await Assert.That(GeneratorTestHarness.HasLocatedDiagnostic(diagnostics, "AUTOREDI012")).IsTrue();
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "global::Probe.AbstractService")).IsFalse();
    }

    [Test]
    public async Task GroupMarkup_GeneratesValidSource()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "safe&<value> */\nnext")]
            public class Grouped { }
            """);

        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(sources[Main]).Contains("&amp;").And.Contains("&lt;").And.Contains("&gt;");
    }

    [Test]
    public async Task InterfaceTypesProperty_TakesPrecedence()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IOld { }
            public interface INew { }

            [global::Autoredi.Attributes.Autoredi(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
                typeof(IOld),
                InterfaceTypes = new[] { typeof(INew) })]
            public class Store : IOld, INew { }
            """);

        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(sources[Main]).Contains("global::Probe.INew");
        await Assert.That(sources[Main]).DoesNotContain("global::Probe.IOld");
    }

    [Test]
    public async Task CamelCaseGroup_DoesNotWarn()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "myGroup")]
            public class Grouped { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI018")).IsFalse();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "AddAutorediServicesMyGroup")).IsTrue();
    }

    [Test]
    public async Task DuplicateInterfaces_EmitOneDescriptor()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            public interface IFoo { }

            [global::Autoredi.Attributes.Autoredi(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
                InterfaceTypes = new[] { typeof(IFoo), typeof(IFoo) })]
            public class Store : IFoo { }
            """);

        var source = sources[Main];
        var descriptor = "ServiceDescriptor.Singleton<global::Probe.IFoo, global::Probe.Store>()";
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(source.Split(descriptor, StringSplitOptions.None).Length - 1).IsEqualTo(2);
    }

    [Test]
    public async Task PunctuatedAssembly_GeneratesValidNamespace()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi]
            public class Service { }
            """, "Probe-Assembly", generateAggregator: true);

        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(sources[Main]).Contains("namespace ProbeAssembly.Autoredi;");
        await Assert.That(sources["AutorediServices.All.g.cs"])
            .Contains("global::ProbeAssembly.Autoredi.AutorediServiceCollectionExtensions");
    }

    [Test]
    public async Task AssemblyCollision_PreservesAssemblyMethod()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "Probe")]
            public class Grouped { }
            """);

        var source = sources[Main];
        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI023")).IsTrue();
        await Assert.That(source.Contains("AddAutorediServicesProbe", StringComparison.Ordinal)).IsTrue();
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
    }

    [Test]
    public async Task AggregatorCollision_Compiles()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi(group: "Probe")]
            public class Grouped { }
            """, generateAggregator: true);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI023")).IsTrue();
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(sources["AutorediServices.All.g.cs"])
            .Contains("AddAutorediServicesProbe(services);");
    }

    [Test]
    public async Task ReservedAssemblyName_UsesFallbackMethod()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [global::Autoredi.Attributes.Autoredi]
            public class Service { }
            """, "All", generateAggregator: true);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI023")).IsFalse();
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(sources[Main]).Contains("AddAutorediServicesAllAssembly");
        await Assert.That(sources["AutorediServices.All.g.cs"])
            .Contains("AddAutorediServicesAllAssembly(services);");
    }
}
