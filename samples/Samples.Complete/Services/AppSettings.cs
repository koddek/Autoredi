using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Samples.Complete.Services;

/// <summary>
/// Application settings as a singleton concrete service.
/// </summary>
[Autoredi(ServiceLifetime.Singleton)]
public class AppSettings
{
    public string ApplicationName => "Autoredi Complete Demo";
    public string Version => "2.0.0";
    public DateTime StartedAt { get; } = DateTime.UtcNow;
    public bool IsDebugMode { get; set; } = true;
}
