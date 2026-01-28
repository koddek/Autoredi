# Autoredi Samples and Benchmarks Implementation Plan

## Overview

This plan outlines the creation of a `samples` folder (repurposing the existing `sandbox`) and a new `benchmarks` folder to demonstrate Autoredi's features and compare its performance against manual DI registration.

## Project Structure

```
Autoredi/
├── samples/                          # Renamed from sandbox
│   ├── Samples.Common/               # Shared interfaces and models
│   ├── Samples.Basic/                # Concrete service registration demo
│   ├── Samples.SingleInterface/      # Single interface implementation demo
│   ├── Samples.KeyedServices/        # Multiple keyed implementations demo
│   └── Samples.Complete/             # Full demo combining all features
├── benchmarks/
│   └── Autoredi.Benchmarks/          # BenchmarkDotNet performance comparison
├── src/
│   ├── Autoredi/
│   └── Autoredi.Generators/
└── tests/
    └── Autoredi.Tests/
```

## 1. Samples Folder

### 1.1 Samples.Common
**Purpose**: Shared contracts used across all sample projects.

**Files to Create**:
- `Samples.Common.csproj` - Project file referencing Autoredi
- `Interfaces/ILogger.cs` - Simple logging interface
- `Interfaces/INotificationService.cs` - Notification service interface
- `Interfaces/IRepository.cs` - Generic repository interface
- `Models/AppConfig.cs` - Configuration model

### 1.2 Samples.Basic
**Purpose**: Demonstrate concrete service registration without interfaces.

**Files to Create**:
- `Samples.Basic.csproj` - Project file
- `Services/AppConfigService.cs` - Singleton concrete service with `[Autoredi]`
- `Program.cs` - Demo showing resolution

**Features Demonstrated**:
- `[Autoredi(ServiceLifetime.Singleton)]` on concrete class
- Direct type resolution from container

### 1.3 Samples.SingleInterface
**Purpose**: Demonstrate single interface implementation registration.

**Files to Create**:
- `Samples.SingleInterface.csproj` - Project file
- `Services/ConsoleLogger.cs` - Implements ILogger from Common
- `Program.cs` - Demo showing interface-based resolution

**Features Demonstrated**:
- `[Autoredi(ServiceLifetime.Transient, typeof(ILogger))]`
- Interface-to-implementation mapping

### 1.4 Samples.KeyedServices
**Purpose**: Demonstrate multiple implementations of the same interface with keys.

**Files to Create**:
- `Samples.KeyedServices.csproj` - Project file
- `Services/EmailNotificationService.cs` - Implements INotificationService
- `Services/SmsNotificationService.cs` - Implements INotificationService
- `Services/PushNotificationService.cs` - Implements INotificationService
- `Constants/ServiceKeys.cs` - Key constants
- `Program.cs` - Demo showing keyed resolution

**Features Demonstrated**:
- `[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), "email")]`
- `GetKeyedService<T>()` usage
- `FromKeyedServices` attribute usage

### 1.5 Samples.Complete
**Purpose**: Comprehensive demo combining all features in a realistic scenario.

**Files to Create**:
- `Samples.Complete.csproj` - Project file referencing all Common and other samples
- `Controllers/NotificationController.cs` - Uses keyed services
- `Services/OrderService.cs` - Uses multiple dependencies
- `Services/UserService.cs` - Demonstrates scoped lifetime
- `Program.cs` - Full application demo

**Features Demonstrated**:
- All lifetimes (Singleton, Scoped, Transient)
- Cross-assembly service resolution
- Constructor injection with keyed services
- Func<string, T> factory pattern

## 2. Benchmarks Folder

### 2.1 Autoredi.Benchmarks
**Purpose**: Compare Autoredi vs manual DI registration performance.

**Files to Create**:
- `Autoredi.Benchmarks.csproj` - Project file with BenchmarkDotNet
- `Benchmarks/RegistrationBenchmarks.cs` - Container build benchmarks
- `Benchmarks/ResolutionBenchmarks.cs` - Service resolution benchmarks
- `Benchmarks/MemoryBenchmarks.cs` - Memory allocation benchmarks
- `Services/AutorediServices.cs` - Services with [Autoredi] attributes
- `Services/ManualServices.cs` - Identical services without attributes
- `Program.cs` - Benchmark runner

**Benchmark Scenarios**:

#### RegistrationBenchmarks
| Scenario | Description |
|----------|-------------|
| `Autoredi_Registration` | Build container using AddAutorediServices() |
| `Manual_Registration` | Build container with manual AddSingleton/Scoped/Transient calls |

#### ResolutionBenchmarks
| Scenario | Description |
|----------|-------------|
| `Autoredi_Resolve_Singleton` | Resolve singleton service (Autoredi) |
| `Manual_Resolve_Singleton` | Resolve singleton service (Manual) |
| `Autoredi_Resolve_Transient` | Resolve transient service (Autoredi) |
| `Manual_Resolve_Transient` | Resolve transient service (Manual) |
| `Autoredi_Resolve_Keyed` | Resolve keyed service (Autoredi) |
| `Manual_Resolve_Keyed` | Resolve keyed service (Manual) |

#### MemoryBenchmarks
| Scenario | Description |
|----------|-------------|
| `Autoredi_Memory` | Memory used by Autoredi registration |
| `Manual_Memory` | Memory used by manual registration |

**Expected Output**:
Benchmark results showing:
- Mean execution time
- Allocated memory
- Gen 0/1/2 collections
- Comparison summary table

## 3. Solution Updates

Update `Autoredi.sln` to:
1. Remove old sandbox project references
2. Add all new sample projects under `samples` solution folder
3. Add benchmark project under `benchmarks` solution folder

## 4. File Migration Strategy

### From sandbox/ to samples/:
- `sandbox/Infrastructure/IFoo.cs` → `samples/Samples.Common/Interfaces/IFoo.cs`
- `sandbox/FooGame/Foo.cs` → Split across relevant sample projects
- `sandbox/ConsoleApp/Program.cs` → Repurpose into `Samples.Complete/Program.cs`

## 5. Implementation Order

1. Create folder structure
2. Create Samples.Common project
3. Create individual sample projects (Basic, SingleInterface, KeyedServices)
4. Create Samples.Complete project
5. Create Autoredi.Benchmarks project
6. Update solution file
7. Delete old sandbox folder
8. Run and verify all projects build
9. Run benchmarks and generate summary

## 6. Success Criteria

- [ ] All sample projects build successfully
- [ ] All sample projects run and demonstrate expected output
- [ ] Benchmarks run without errors
- [ ] Benchmark results show comparison between Autoredi and manual registration
- [ ] Solution file is properly updated
- [ ] Old sandbox folder is removed
