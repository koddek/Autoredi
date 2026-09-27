namespace Autoredi.Tests.Fixtures;

/// <summary>
/// Base class for tests that resolve services from a container built purely out of the
/// generated registration of this test assembly. It exists so the build/dispose boilerplate
/// lives in one place instead of being copy-pasted into every test class; tests that need
/// extra manual registrations should build their own container instead of deriving.
/// </summary>
public abstract class AutorediContainerTest : IDisposable
{
    /// <summary>
    /// Provider built from <c>AddAutorediServices()</c> for this test assembly's own generated
    /// extension class. Disposed together with the test instance.
    /// </summary>
    protected ServiceProvider Provider { get; }

    protected AutorediContainerTest()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        Provider = services.BuildServiceProvider();
    }

    public void Dispose() => Provider.Dispose();
}
