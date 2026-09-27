# Changelog

All notable changes to this project will be documented in this file.

## [0.6.0] - 2026-09-27

### Added
- **The package multi-targets `netstandard2.0` and `net10.0`.** Previously only `lib/net10.0` shipped, so any project targeting net8.0, net9.0, or an older class library TFM failed to restore with NU1202. The attribute has no runtime dependencies beyond MEDI abstractions, so the extra target costs nothing. `scripts/verify-package-smoke.mjs` now restores and runs a real net8.0 consumer to prove it.
- **`AUTOREDI001` build error** when `dotnet pack` runs without `Autoredi.snk`, so an unsigned package can never reach a feed.
- **Pull request CI.** Previously only pushes to `main` were verified, so a pull request could sit broken. Both verification scripts and an artifact check now run on pull requests too.

### Fixed
- **Interface implementations contributed by different assemblies are no longer dropped.** Interface multiplicity was counted per assembly at generation time, so a second assembly's registration of the same interface used `TryAdd` against an already-registered service type and became a no-op. `AddAutorediServicesAll()` silently lost every implementation except the first assembly's. Interface registrations now go through a generated `TryAddAutorediDescriptor` helper that keeps every distinct implementation type, so `IEnumerable<IFoo>` resolves the full set across the whole reference graph.
- **A hand-written instance or factory registration still wins over a generated one.** The fix above would otherwise have let a generated descriptor outrank `services.AddSingleton<IFoo>(myFake)`, because a `TryAddEnumerable` only recognises duplicates by implementation type. The helper distinguishes the two cases: no implementation type means a human decided what resolves, so the generated descriptor is skipped.
- **Building no longer requires the strong-name key.** `Directory.Build.props` gated signing on `$(IsTestProject)`, which nothing ever set, so every project was signed and a contributor without the gitignored key hit `CS7027`. Signing is now conditional on the key file existing, and packing is a separate, explicit step.
- **CI no longer packs a project that produces nothing.** `Autoredi.Generators.csproj` is `IsPackable=false`, so its `dotnet pack` step always emitted no package while looking like it did work.

### Changed
- `NoWarn CS1591` is no longer applied repository-wide. XML documentation is now generated (and CS1591 enforced) only for the packable library, so missing public API docs fail the build; the analyzer, samples, tests, and benchmarks opt out explicitly.
- Dependency versions are declared once in `Directory.Build.props` and `$(FlowgenVersion)` replaces the repeated literal Flowgen pin. Twelve projects previously repeated the MEDI and SourceLink versions as no-op `Update` items, which made every dependency bump a twelve-file edit. `scripts/verify-stabilization.mjs` now checks that consistency instead of hardcoding version numbers, so a legitimate bump no longer fails the verifier.
- Interface registrations are emitted as `TryAddAutorediDescriptor(services, ServiceDescriptor.X<I, Impl>())` instead of a bare `TryAdd` or `TryAddEnumerable`. Generated output and the XML docs on every generated method were updated to describe the rule.
- TUnit updated to 1.70.1.
- Flowgen updated to 0.12.0. No code changes were required: 0.12.0 removed `context.Flow()`, the `Flow.Create(context)` overload, `AttributeContext.Symbol`, and the never-reported `FlowgenDiagnostics` descriptors, none of which this generator used. `$(FlowgenVersion)` in `Directory.Build.props` drives the version, and the package smoke test confirms the packed analyzer payload really is the pinned version.

### Removed
- `tests/Autoredi.Tests/Diagnostics/ValidationTests.cs`: it held no diagnostic tests, and its assertions were either tautologies (`IsNotNull()` on a freshly constructed `ServiceCollection`) or exact duplicates of tests in `Core/`, `Features/`, and `Integration/`. The one distinct assertion moved to `RegistrationSemanticsTests`.
- Stale agent working notes (`codex-plan.md`, `plans/`) that the repository's own `.gitignore` already listed as unwanted and that had all been implemented.

## [0.5.3] - 2026-09-24

### Changed
- Updated Flowgen to 0.11.0 and migrated the generator to its current pipeline API.
- Updated direct MEDI, SourceLink, and TUnit dependencies; pinned Imposter.
- Aligned CI with the .NET 10 solution and added test/sample verification.
- Hardened generated namespaces, XML documentation, interface multiplicity, and invalid implementation diagnostics.
- Clarified TryAdd, priority, and cross-assembly registration behavior in the consumer documentation.

## [0.5.2] - 2026-09-01

### Changed
- Intermediate release: dependency refresh and groundwork for the Flowgen pipeline migration completed in 0.5.3.

## [0.5.1] - 2026-08-25

### Changed
- Bump dependencies to latest patch releases.

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
