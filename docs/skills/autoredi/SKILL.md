---
name: autoredi
description: Register .NET services in Microsoft.Extensions.DependencyInjection at compile time with [Autoredi] attributes and generated extension methods (TryAdd semantics, keyed services, groups, multi-interface). Use when a project references the Autoredi NuGet package, when writing or fixing classes decorated with [Autoredi], when calling AddAutorediServices* methods, or when diagnosing AUTOREDI00x build errors.
---

# Autoredi

Compile-time DI registration for Microsoft.Extensions.DependencyInjection. Put `[Autoredi]` on a class; a source generator emits extension methods into that assembly. No reflection, AOT-safe.

## Mental model

1. Decorate a class → generator records it.
2. Each assembly gets `<AssemblyName>.Autoredi.AutorediServiceCollectionExtensions` (public static partial class) with extension methods on `IServiceCollection`.
3. Consumer code must `using <AssemblyName>.Autoredi;` — child namespaces are not auto-visible.
4. Call the generated method(s) during composition root setup.

## Attribute quick reference

```csharp
[Autoredi(lifetime, interfaceType, serviceKey, group, priority)]
[Autoredi(..., InterfaceTypes = new[] { typeof(A), typeof(B) })]
```

| Parameter | Default | Effect |
|---|---|---|
| `lifetime` | `Transient` | Singleton / Scoped / Transient. Out-of-range values fail the build (AUTOREDI010). |
| `interfaceType` | null | Register as this service type instead of self. Must be an interface the class implements (AUTOREDI011 / AUTOREDI007 otherwise). |
| `serviceKey` | null | Keyed registration (MEDI 8+). Resolve via `GetKeyedService(key)` or `[FromKeyedServices]`. |
| `group` | null | Partitions registrations into separate generated methods. |
| `priority` | 0 | Higher emitted first within its group; ties break alphabetically by type name. |
| `InterfaceTypes` (property) | null | One descriptor per interface; replaces `interfaceType`; no self-registration when non-empty. |

Syntax rules:
- Positional args cannot skip: `[Autoredi(typeof(X))]` is illegal — pass lifetime first or use named form for later params (`group:`, `priority:`).
- `InterfaceTypes` is a settable property (named argument): `InterfaceTypes = [typeof(IRepo), typeof(ICache)]`.

## Generated methods

| Method | Registers |
|---|---|
| `AddAutorediServices()` | This assembly's ungrouped services (everything if no groups exist). |
| `AddAutorediServices{Group}()` | Only this assembly's named group. Sanitized identifiers warn (AUTOREDI018); name collisions error and skip (AUTOREDI023). |
| `AddAutorediServices{Assembly}()` | Every group of this assembly. |
| `AddAutorediServicesAll()` | Executables only: current assembly + every referenced assembly that contributes registrations (referenced assemblies are detected via their generated marker class). |

## TryAdd contract

Generated bodies use TryAdd semantics — they fill gaps, never override:

- Single implementation per service type/key: `services.TryAdd(ServiceDescriptor.Scoped<IFoo, Foo>());`
- Multiple implementations of one interface: `TryAddEnumerable` per pair so all coexist for `IEnumerable<IFoo>`.
- Self registrations: `services.TryAddSingleton<T>();`
- Keyed variants mirror the same split.

Consequences:
- Manual registration made BEFORE the call always wins — standard test override technique.
- Double-calls are idempotent (descriptor count unchanged).
- Priority matters only against the gap-filling order among competing Autoredi registrations.

## Canonical usage

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Autoredi; // required to see generated methods

[Autoredi(ServiceLifetime.Singleton)]                       // self, singleton
public sealed class AppConfig { }

[Autoredi(ServiceLifetime.Scoped, typeof(IUserRepo))]       // interface mapping
public sealed class SqlUserRepo : IUserRepo { }

[Autoredi(ServiceLifetime.Transient, typeof(INotifier), "email", group: "Notify")]
public sealed class EmailNotifier : INotifier { }           // keyed + grouped

var services = new ServiceCollection();
services.AddAutorediServices();          // or AddAutorediServicesAll() in executables
await using var provider = services.BuildServiceProvider();

IUserRepo repo = provider.GetRequiredService<IUserRepo>();
INotifier email = provider.GetRequiredKeyedService<INotifier>("email");
```

Multi-interface in one attribute:

```csharp
[Autoredi(ServiceLifetime.Scoped, InterfaceTypes = [typeof(IRepo), typeof(ICache)])]
public sealed class RedisStore : IRepo, ICache { }
// emits TryAdd(ServiceDescriptor.Scoped<IRepo, RedisStore>()) + same for ICache
// (TryAddEnumerable instead when another class also implements either interface)
```

Cross-assembly selective registration from a library's generated class:

```csharp
using InfrastructureAutoredi = MyApp.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;
services.AddAutorediServices();                                  // app defaults
InfrastructureAutoredi.AddAutorediServicesStorage(services);     // one library group
```

## Diagnostics

| Id | Severity | Meaning |
|---|---|---|
| AUTOREDI007 | Error | Class does not implement requested interface. |
| AUTOREDI010 | Error | Invalid ServiceLifetime value. |
| AUTOREDI011 | Error | Requested service type is not an interface (or null). |
| AUTOREDI018 | Warning | Group name sanitized into a valid identifier; generated method renamed accordingly. |
| AUTOREDI023 | Error | Two generated methods would share a name ("All" reserved, group vs assembly fragment). Later registrations skipped until renamed. |

Fix guidance: rename the group/assembly side, implement the interface, correct the enum value. Skipped registrations never appear in generated output — do not paper over AUTOREDI023 by hand-writing duplicate methods.

## Gotchas

- Generated namespace is per-assembly: missing `using X.Autoredi;` surfaces as CS1061 "no extension method AddAutorediServices".
- `AddAutorediServicesAll()` exists only in executable projects; libraries must aggregate explicitly or expose their own methods.
- Group methods are per-assembly by design; there is no automatic cross-assembly group fan-out.
- MEDI resolves each service type independently — two interfaces backed by one singleton class produce two instances unless you register an instance manually (standard MEDI behavior, not an Autoredi quirk).

## Verify changes

```
dotnet build <solution>
dotnet run --project <test-project>
```
