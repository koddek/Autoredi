---
name: autoredi
description: Register .NET services in Microsoft.Extensions.DependencyInjection at compile time with [Autoredi] attributes and generated extension methods (fill-gaps registration contract, keyed services, open generics, groups, multi-interface, cross-assembly aggregation). Use when a project references the Autoredi NuGet package, when writing or fixing classes decorated with [Autoredi], when calling AddAutorediServices* methods, or when diagnosing AUTOREDI00x build errors.
---

# Autoredi

Compile-time DI registration for Microsoft.Extensions.DependencyInjection. Put `[Autoredi]` on a class; a source generator emits extension methods into that assembly. No reflection, AOT-safe.

## Mental model

1. Decorate a class → generator records it.
2. Each assembly gets `<AssemblyName>.Autoredi.AutorediServiceCollectionExtensions` (public static partial class) with extension methods on `IServiceCollection`.
3. Consumer code must `using <AssemblyName>.Autoredi;` — child namespaces are not auto-visible.
4. Call the generated method(s) during composition root setup.

The package targets `netstandard2.0`, `net10.0`, and `net11.0`, so referencing it works from net8.0/net9.0, .NET 10, .NET 11, and older class libraries too.

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
| `priority` | 0 | Higher emitted first within the selected method; ties break alphabetically by type name. It orders descriptors, not MEDI's single-service winner. |
| `InterfaceTypes` (property) | null | One descriptor per unique interface; replaces `interfaceType`; no self-registration when non-empty. Entries may be open generic definitions (`typeof(IFoo<>)`) when the class is a generic implementation. |

The decorated type must be a non-static, non-abstract, non-generic class with a public constructor, unless it registers open generic service types (see below). Invalid implementation types produce AUTOREDI012.

### Open generics

`typeof(IFoo<>)` requests an open generic service type. The decorated class must be a generic class whose type parameters map positionally to the service type's parameters (`class Repo<A, B> : IRepo<A, B>` works, `class Repo<A, B> : IRepo<B, A>` does not); otherwise the generator reports AUTOREDI027. The request cannot be mixed with closed entries on the same class (AUTOREDI012). Keyed open generics work the same as closed ones. The registration contract is identical: `TryAddAutorediDescriptor` decides via the same per-method index, so every construction follows the same all-implementations-kept rule.

Syntax rules:
- Positional args cannot skip: `[Autoredi(typeof(X))]` is illegal — pass lifetime first or use named form for later params (`group:`, `priority:`).
- `InterfaceTypes` is a settable property (named argument): `InterfaceTypes = [typeof(IRepo), typeof(ICache)]`.

## Generated methods

| Method | Registers |
|---|---|
| `AddAutorediServices()` | This assembly's ungrouped services (everything if no groups exist). |
| `AddAutorediServices{Group}()` | Only this assembly's named group. Sanitized identifiers warn (AUTOREDI018); name collisions error and skip (AUTOREDI023). |
| `AddAutorediServices{Assembly}()` | Every group of this assembly. |
| `AddAutorediServicesAll()` | Executables only: current assembly + the complete referenced closure (direct and transitive: every package and project reference) that contributes registrations. Contributing referenced assemblies register in ordinal alphabetical order; the calling assembly registers last, so its registrations win single-service resolution. |

## Registration contract

Generated methods fill gaps and never replace what is already registered.

Self-registration (no `interfaceType`): `services.TryAddSingleton<T>();` — the exact MEDI `TryAdd` guarantee. Keyed self registrations emit the matching `TryAddKeyedTransient/Scoped/Singleton`.

Interface registration (with `interfaceType` or `InterfaceTypes`): the emitted line is
`TryAddAutorediDescriptor(services, autorediIndex, ServiceDescriptor.<Lifetime><IFoo, Foo>());`. The calling
method builds an `AutorediIndex` (a hash set of the service type + key + implementation type combinations
already registered) so the check is O(1) per descriptor instead of a rescan. Resolution semantics:

| Already present for that service type + key | Result |
|---|---|
| nothing | descriptor added |
| the same implementation type (earlier generated call, or your own `Add<IFoo, Foo>()`) | skipped, no duplicate |
| a different implementation type (yours, or another assembly's) | added, so every implementation resolves through `IEnumerable<IFoo>` |
| an instance or factory registration (`ImplementationType` is null) | skipped, your registration keeps winning |

What this means in practice:
- `AddAutorediServicesAll()` keeps implementations of the same interface contributed by different
  assemblies. Interface multiplicity is judged per assembly at generation time, so the runtime check
  is what stops one assembly's registration from being dropped as a duplicate of another's.
- `services.AddSingleton<IFoo>(fake); services.AddAutorediServices();` keeps `fake` — the standard
  test-seam pattern. Same for a factory registration.
- Calling a generated method twice changes nothing.
- Resolving a single `IFoo` still follows MEDI's last-registration order. Use keyed registrations when
  the choice must be explicit.

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
// emits one TryAddAutorediDescriptor call per interface, e.g.
//   TryAddAutorediDescriptor(services, autorediIndex, ServiceDescriptor.Scoped<IRepo, RedisStore>());
```

Open generic registration in one attribute:

```csharp
[Autoredi(ServiceLifetime.Scoped, typeof(IRepository<>))]
public sealed class SqlRepository<T> : IRepository<T> { }
// one open generic descriptor; every closed construction resolves afterwards:
//   provider.GetRequiredService<IRepository<User>>()
// keyed: [Autoredi(ServiceLifetime.Scoped, typeof(IRepository<>), "sql")]
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
| AUTOREDI012 | Error | Decorated implementation type cannot be registered by MEDI. |
| AUTOREDI018 | Warning | Group name contains characters that require identifier sanitization. |
| AUTOREDI023 | Error | Two generated methods would share a name ("All" reserved, group vs assembly fragment). Later registrations skipped until renamed. |
| AUTOREDI024 | Error | Interface is not accessible from generated code (file-local, private, or nested inside an invisible type). |
| AUTOREDI025 | Warning | Referenced assembly has the expected marker class name but no compatible `AddAutorediServices{Assembly}(IServiceCollection)` method; it is skipped. Upgrade that assembly or call its methods directly. |
| AUTOREDI027 | Error | Implementation type parameters do not map positionally to the requested open generic service type. |

Build-time error outside the generator: `AUTOREDI001` means `dotnet pack` ran without `Autoredi.snk`
next to `Directory.Build.props` and would have published an unsigned package. A plain `dotnet build`
never needs the key.

Fix guidance: use a concrete implementation with a public constructor, rename the group/assembly side, implement the interface, or correct the enum value. Skipped registrations never appear in generated output — do not paper over AUTOREDI023 by hand-writing duplicate methods.

## Gotchas

- Generated namespace is per-assembly: missing `using X.Autoredi;` surfaces as CS1061 "no extension method AddAutorediServices".
- The decorated type must be a concrete, non-generic class with a public constructor, unless every requested service type is an open generic definition (`typeof(IFoo<>)`), in which case the class must be a generic class whose type parameters map positionally to the interface's (otherwise AUTOREDI027).
- An assembly whose method fragment would be `All` uses the `AddAutorediServicesAllAssembly` fallback for its assembly-wide method.
- Assembly names with characters legal in identifiers are used as-is; everything else is escaped losslessly (`-` → `_u002d_`, `_` → `__`), so `Lib-One` generates `namespace Lib_u002d_One.Autoredi;` and can never collide with a `LibOne` assembly.
- `AddAutorediServicesAll()` exists only in executable projects; libraries must aggregate explicitly or expose their own methods.
- It aggregates the complete **transitive** reference closure: every referenced package and project, direct or indirect, that contributes registrations (detected via the generated marker class; an assembly built with Autoredi 0.4 or earlier has no marker and is silently skipped).
- Transitivity comes from the SDK: for typical `net8.0`+ builds the compiler sees the full package/project closure directly, so the probe finds libraries referenced only transitively. References whose assemblies carry no Autoredi generated code are ignored.
- Group methods are per-assembly. `AddAutorediServices{Assembly}()` is the per-library "register everything this library contributes" call; `AddAutorediServicesAll()` is the single call for the whole closure.
- Two libraries can generate a method with the same name (e.g. two `Storage` groups). Importing both Autoredi namespaces and calling it unqualified is CS0121; alias one class (`using InfrastructureAutoredi = MyApp.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;`).
- Group methods are per-assembly by design; there is no automatic cross-assembly group fan-out.
- MEDI resolves each service type independently — two interfaces backed by one singleton class produce two instances unless you register an instance manually (standard MEDI behavior, not an Autoredi quirk).
- `Priority` orders descriptors inside one generated method. It does not decide which implementation MEDI returns for a single service; use keyed registrations for that.
- A generated interface registration is skipped when you already registered that service type with an instance or a factory. That is deliberate, but it also means adding a factory for `IFoo` silently disables every generated `IFoo` registration in the container.

## Verify changes

```
dotnet build <solution>
dotnet run --project <test-project>
```
