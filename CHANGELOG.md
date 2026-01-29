# Changelog

All notable changes to this project will be documented in this file.

## [0.2.0] - 2026-01-28

### Added
- **Group Property**: Organize services into logical groups with selective registration.
  - `AddAutorediServices()` still registers all services (backward compatible).
  - New group-specific methods: `AddAutorediServicesFirebase()`, `AddAutorediServicesAccount()`, etc.
  - Services without a group go to `AddAutorediServicesDefault()`.
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
