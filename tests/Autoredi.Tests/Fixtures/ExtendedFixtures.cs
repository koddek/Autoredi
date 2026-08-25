using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Fixtures;

// --- Multiple non-keyed implementations for IEnumerable<T> ---
public interface IReportGenerator
{
    string Generate();
}

[Autoredi(ServiceLifetime.Transient, typeof(IReportGenerator))]
public class PdfReportGenerator : IReportGenerator
{
    public string Generate() => "pdf";
}

[Autoredi(ServiceLifetime.Transient, typeof(IReportGenerator))]
public class CsvReportGenerator : IReportGenerator
{
    public string Generate() => "csv";
}

// --- Special-character keyed service ---
public interface ISpecialKeyService
{
    string Handle();
}

[Autoredi(ServiceLifetime.Singleton, typeof(ISpecialKeyService), "quote\"and\\slash")]
public class SpecialKeyService : ISpecialKeyService
{
    public string Handle() => "special";
}

// --- Scoped and Transient keyed services ---
public interface IScopedKeyedService
{
    Guid Id { get; }
}

[Autoredi(ServiceLifetime.Scoped, typeof(IScopedKeyedService), "scoped-a")]
public class ScopedKeyedServiceA : IScopedKeyedService
{
    public Guid Id { get; } = Guid.NewGuid();
}

[Autoredi(ServiceLifetime.Transient, typeof(IScopedKeyedService), "transient-b")]
public class TransientKeyedServiceB : IScopedKeyedService
{
    public Guid Id { get; } = Guid.NewGuid();
}

// --- Priority tie-break: same priority, alphabetical ordering ---
[Autoredi(ServiceLifetime.Transient, group: "PriorityTie", priority: 5)]
public class AlphaService { }

[Autoredi(ServiceLifetime.Transient, group: "PriorityTie", priority: 5)]
public class BetaService { }

[Autoredi(ServiceLifetime.Transient, group: "PriorityTie", priority: 5)]
public class GammaService { }

// --- Self vs interface distinction ---
public interface ISelfCheck { }

[Autoredi(ServiceLifetime.Transient, typeof(ISelfCheck))]
public class SelfCheckImpl : ISelfCheck { }
