using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Fixtures;

// --- Multi-interface registration fixture ---

public interface IAuditTrail
{
    string Trail();
}

public interface ITelemetrySink
{
    string Signal();
}

[Autoredi(ServiceLifetime.Singleton, InterfaceTypes = [typeof(IAuditTrail), typeof(ITelemetrySink)])]
public class CompositeAuditor : IAuditTrail, ITelemetrySink
{
    public string Trail() => "trail";
    public string Signal() => "signal";
}
