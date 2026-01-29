using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Benchmarks.Services;

// --- Group A ---
[Autoredi(ServiceLifetime.Singleton, group: "GroupA", priority: 100)]
public class GroupAService1;

[Autoredi(ServiceLifetime.Transient, group: "GroupA", priority: 50)]
public class GroupAService2;

// --- Group B ---
[Autoredi(ServiceLifetime.Singleton, group: "GroupB")]
public class GroupBService1;

// --- Default Group ---
[Autoredi(ServiceLifetime.Transient)]
public class DefaultBenchmarkService;
