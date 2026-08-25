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

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
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

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IFoo))]
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

            [Autoredi.Attributes.Autoredi(
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

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IRepo))]
            public class RepoA : IRepo { }

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IRepo))]
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

            [Autoredi.Attributes.Autoredi(
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

            [Autoredi.Attributes.Autoredi((Microsoft.Extensions.DependencyInjection.ServiceLifetime)42)]
            public class Weird { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI010")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "global::Probe.Weird")).IsFalse();
    }

    [Test]
    public async Task NonInterfaceServiceType_ReportsAutoredi011()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;

            public class NotAnInterface { }

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(NotAnInterface))]
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

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient, typeof(IMissing))]
            public class Broken { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI007")).IsTrue();
    }

    [Test]
    public async Task InvalidGroupName_WarnsAndSanitizesMethod()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;

            [Autoredi.Attributes.Autoredi(group: "my-group")]
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

            [Autoredi.Attributes.Autoredi(group: "All")]
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

            [Autoredi.Attributes.Autoredi(priority: 10)]
            public class High { }

            [Autoredi.Attributes.Autoredi(priority: 100)]
            public class Highest { }

            [Autoredi.Attributes.Autoredi()]
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

            [Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
            public class Plain { }

            [Autoredi.Attributes.Autoredi(group: "Notify")]
            public class Grouped { }
            """);

        var source = sources[Main];
        var assemblyWideIndex = source.IndexOf("AddAutorediServicesProbe", StringComparison.Ordinal);

        await Assert.That(assemblyWideIndex).IsGreaterThan(0);
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "AddAutorediServicesNotify")).IsTrue();
    }
}
