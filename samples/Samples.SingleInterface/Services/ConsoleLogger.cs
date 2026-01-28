using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;

namespace Samples.SingleInterface.Services;

/// <summary>
/// Demonstrates a single interface implementation registered with Autoredi.
/// </summary>
[Autoredi(ServiceLifetime.Transient, typeof(ILogger))]
public class ConsoleLogger : ILogger
{
    private readonly Guid _instanceId = Guid.NewGuid();

    public void Log(string message)
    {
        Console.WriteLine($"[{_instanceId:N}] [LOG]: {message}");
    }
}
