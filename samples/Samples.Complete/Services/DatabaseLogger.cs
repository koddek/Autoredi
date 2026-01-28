using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;

namespace Samples.Complete.Services;

/// <summary>
/// Database logger implementation registered as scoped.
/// </summary>
[Autoredi(ServiceLifetime.Scoped, typeof(ILogger))]
public class DatabaseLogger : ILogger
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly AppSettings _settings;

    public DatabaseLogger(AppSettings settings)
    {
        _settings = settings;
    }

    public void Log(string message)
    {
        var prefix = _settings.IsDebugMode ? "[DEBUG]" : "[INFO]";
        Console.WriteLine($"[{_instanceId:N}] {prefix} [DB_LOGGER]: {message}");
        Console.WriteLine($"[{_instanceId:N}] {prefix} [DB_LOGGER]: Saving to database...");
    }
}
