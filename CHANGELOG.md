# Changelog

All notable changes to this project will be documented in this file.

## [0.5.0] - 2026-08-25

### Added
- **Agent skill ships in the package**: `skills/Autoredi/SKILL.md` inside the nupkg gives AI coding agents (opencode, Claude Code, etc.) usage guidance; discoverable at `~/.nuget/packages/autoredi/<version>/skills/Autoredi/SKILL.md`.
- **Full XML documentation**: `AutorediAttribute` and every generated extension method now carry complete `<summary>`/`<remarks>`/`<example>` docs, visible through IntelliSense and doc generators.
- **Multi-interface registration**: `[Autoredi(..., InterfaceTypes = [typeof(A), typeof(B)])]` emits one descriptor per interface. Explicit interface list means interfaces-only (no self registration), matching the single `interfaceType` contract.
- **Compile-time diagnostics** (generator now validates instead of emitting broken code):
    - AUTOREDI007 error: decorated class does not implement the requested interface.
    - AUTOREDI010 error: invalid ServiceLifetime value.
    - AUTOREDI011 error: requested service type is not an interface.
    - AUTOREDI018 warning: group name is not a valid C# identifier; method is generated from a sanitized fragment (`"my-group"` → `AddAutorediServicesMyGroup`).
    - AUTOREDI023 error: two generated methods would share one name (group named "All", group fragment equal to the assembly fragment, or duplicate fragments); the later registration set is skipped.

### Changed
- **TryAdd semantics**: generated methods use `TryAdd*` / `TryAddEnumerable(ServiceDescriptor.*)` instead of `Add*`.
    - Calling a generated method twice no longer duplicates descriptors.
    - Generated registrations fill gaps and never override services registered manually before the call (previously last-wins).
    - Multiple implementations of the same unkeyed interface keep coexisting (`IEnumerable<T>` resolution unchanged) because interface registrations use `TryAddEnumerable`, which matches on service type + implementation type.
- Generator performance: referenced-assembly discovery probes each reference's generated marker type instead of walking every type of every referenced assembly on each compilation update.
- Pinned `Microsoft.Extensions.DependencyInjection(.Abstractions)` to 10.0.2 in `Directory.Build.props` (was floating `Version="*"`).

### Fixed
- Service keys containing quotes or backslashes no longer produce uncompilable code (keys are emitted as properly escaped literals).
- Group names that are not valid identifiers no longer break the build; they are sanitized with a naming warning.
- README removed an incorrect claim that group methods automatically include same-group services from referenced assemblies; group methods are per-assembly. Use `AddAutorediServicesAll()` for cross-assembly registration, or call referenced assemblies' generated classes directly (see the modular sample).
- Restored `Samples.Modular.App` (referenced by the solution but missing from disk); samples now use local project references instead of a stale published package version.

## [0.4.11] - 2026-03-06

### Fixed
- Stopped shipping `Microsoft.Extensions.DependencyInjection.Abstractions.dll` inside the NuGet `analyzers/dotnet/cs` folder.
- Trimmed package dependencies so the main library only depends on `Microsoft.Extensions.DependencyInjection.Abstractions`.

## [0.2.0] - 2026-01-28

### Added
- **Group Property**: Organize services into logical groups with selective registration.
    - `AddAutorediServices()` registers only the default (ungrouped) services.
    - New group-specific methods: `AddAutorediServicesFirebase()`, `AddAutorediServicesAccount()`, etc.
    - `AddAutorediServices{AssemblyName}()` registers every service that assembly contributes (e.g., `Samples.Modular.App` becomes `AddAutorediServicesSamplesModularApp`).
    - `AddAutorediServicesAll()` registers services from this assembly and any referenced assemblies that define Autoredi registrations.
  - Global aggregation: Group methods automatically include services from the same group in referenced assemblies.
- **Priority Property**: Control registration order within groups using `priority` (int).
  - Higher values are registered first (e.g., 100 before 0).
  - Default priority is 0.
  - Useful for controlling service registration order for decorators or overrides.
- **Modular Sample**: Added `Samples.Modular` demonstrating cross-assembly grouping and priority.
- **Performance Benchmarks**: Added `GroupingBenchmarks` to measure selective registration overhead.
  - Selective registration is faster than full registration for large containers.
  - Priority sorting adds zero runtime overhead (pre-sorted in generated code).

### Changed
- `AutorediAttribute` now accepts optional `group` and `priority` parameters.
- Generated extension methods now include group-specific registration methods.
- Version bumped to 0.2.0.

### Backward Compatibility
- ✅ All existing code continues to work without changes.
- ✅ New parameters are optional with sensible defaults.
- ✅ `AddAutorediServices()` behavior remains unchanged (registers all services).
