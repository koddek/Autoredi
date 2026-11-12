namespace Autoredi.Tests.Generator;

/// <summary>
/// Generator-driver tests for open generic service registrations (<c>typeof(IFoo&lt;&gt;)</c>).
/// </summary>
public class OpenGenericRegistrationTests
{
    [Test]
    public async Task KeyedOpenGeneric_EmitsKeyedOpenGenericDescriptor()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;
            public interface IRepo<T> { }
            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton, typeof(IRepo<>), "sql")]
            public class Repo<T> : IRepo<T> { }
            """);

        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(GeneratorTestHarness.SourceContains(
            sources,
            "ServiceDescriptor.KeyedSingleton(typeof(global::Probe.IRepo<>), \"sql\", typeof(global::Probe.Repo<>))")).IsTrue();
    }

    [Test]
    public async Task MixedOpenAndClosedRequests_OnGenericImplementation_ReportsAutoredi012()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;
            public interface IRepo<T> { }
            public interface ICache { }
            [global::Autoredi.Attributes.Autoredi(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
                InterfaceTypes = new[] { typeof(IRepo<>), typeof(ICache) })]
            public class Store<T> : IRepo<T>, ICache { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI012")).IsTrue();
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
    }

    [Test]
    public async Task OpenGenericRequest_OnNonGenericImplementation_ReportsAutoredi027()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;
            public interface IRepo<in T> { }
            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IRepo<>))]
            public class Repo : IRepo<int> { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI027")).IsTrue();
    }

    [Test]
    public async Task NonInterfaceOpenGeneric_ReportsAutoredi011()
    {
        var (diagnostics, _) = GeneratorTestHarness.Run("""
            namespace Probe;
            public class Repo<T> { }

            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(System.Collections.Generic.List<>))]
            public class Repo2<T> : System.Collections.Generic.List<T> { }
            """);

        await Assert.That(GeneratorTestHarness.HasDiagnostic(diagnostics, "AUTOREDI011")).IsTrue();
    }

    [Test]
    public async Task MultipleOpenGenericImplementations_SameInterface_BothSurvive()
    {
        var (diagnostics, sources) = GeneratorTestHarness.Run("""
            namespace Probe;
            public interface IRepo<T> { }
            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IRepo<>))]
            public class RepoA<T> : IRepo<T> { }
            [global::Autoredi.Attributes.Autoredi(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, typeof(IRepo<>))]
            public class RepoB<T> : IRepo<T> { }
            """);

        // Both implementations must reach the generated file; the cross-assembly
        // registration contract keeps them both for IRepo<T>.
        await Assert.That(GeneratorTestHarness.HasCompilationErrors(diagnostics)).IsFalse();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "typeof(global::Probe.RepoA<>)")).IsTrue();
        await Assert.That(GeneratorTestHarness.SourceContains(sources, "typeof(global::Probe.RepoB<>)")).IsTrue();
    }

}
