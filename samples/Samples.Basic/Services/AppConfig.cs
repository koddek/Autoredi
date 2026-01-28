using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Samples.Basic.Services;

/// <summary>
/// Demonstrates a concrete service registered as a singleton without an interface.
/// This is the simplest Autoredi use case.
/// </summary>
[Autoredi(ServiceLifetime.Singleton)]
public class AppConfig
{
    public string ApplicationName => "Autoredi Basic Demo";
    public string Version => "1.0.0";
    public DateTime StartedAt { get; } = DateTime.UtcNow;
}
