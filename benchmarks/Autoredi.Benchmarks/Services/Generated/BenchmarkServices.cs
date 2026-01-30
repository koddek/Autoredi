using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Benchmarks.Services.Generated;

// --- Singleton Group ---
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_0 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_1 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_2 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_3 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_4 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_5 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_6 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_7 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_8 {}
[Autoredi(ServiceLifetime.Singleton, group: "Singletons")] public class Singleton_9 {}

// --- Scoped Group ---
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_0 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_1 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_2 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_3 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_4 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_5 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_6 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_7 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_8 {}
[Autoredi(ServiceLifetime.Scoped, group: "Scopeds")] public class Scoped_9 {}

// --- Transient Group ---
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_0 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_1 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_2 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_3 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_4 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_5 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_6 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_7 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_8 {}
[Autoredi(ServiceLifetime.Transient, group: "Transients")] public class Transient_9 {}

// --- Mixed Priority Group ---
[Autoredi(ServiceLifetime.Singleton, group: "Prioritized", priority: 100)] public class PriorityHigh_0 {}
[Autoredi(ServiceLifetime.Scoped,    group: "Prioritized", priority: 100)] public class PriorityHigh_1 {}
[Autoredi(ServiceLifetime.Transient, group: "Prioritized", priority: 100)] public class PriorityHigh_2 {}
[Autoredi(ServiceLifetime.Singleton, group: "Prioritized", priority: 50)]  public class PriorityMid_0 {}
[Autoredi(ServiceLifetime.Scoped,    group: "Prioritized", priority: 50)]  public class PriorityMid_1 {}
[Autoredi(ServiceLifetime.Transient, group: "Prioritized", priority: 50)]  public class PriorityMid_2 {}
[Autoredi(ServiceLifetime.Singleton, group: "Prioritized", priority: 0)]   public class PriorityLow_0 {}
[Autoredi(ServiceLifetime.Scoped,    group: "Prioritized", priority: 0)]   public class PriorityLow_1 {}
[Autoredi(ServiceLifetime.Transient, group: "Prioritized", priority: 0)]   public class PriorityLow_2 {}
[Autoredi(ServiceLifetime.Singleton, group: "Prioritized", priority: 0)] public class PriorityMin_0 {}
