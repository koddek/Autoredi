# Autoredi (Auto Register Dependency Injection)

[![Build Status](https://github.com/koddek/Autoredi/actions/workflows/build-publish-nuget.yml/badge.svg)](https://github.com/koddek/Autoredi/actions/workflows/build-publish-nuget.yml)
[![NuGet Version](https://img.shields.io/nuget/v/Autoredi)](https://www.nuget.org/packages/Autoredi/)
[![GitHub Package Downloads](https://img.shields.io/badge/downloads-0-blue?logo=github)](https://nuget.pkg.github.com/koddek/index.json)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Autoredi is a powerful source generator for .NET that simplifies dependency injection (DI) by automatically registering services in your Microsoft.Extensions.DependencyInjection container. With the `[Autoredi]` attribute, you can declaratively configure services with lifetimes, interfaces, and keys, reducing boilerplate and enhancing maintainability. Whether you're registering simple concrete classes, single interface implementations, or complex keyed services, Autoredi streamlines your DI setup.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
  - [Simple: Registering a Concrete Service](#simple-registering-a-concrete-service)
  - [Intermediate: Single Interface Implementation](#intermediate-single-interface-implementation)
  - [Advanced: Keyed Services for Multiple Implementations](#advanced-keyed-services-for-multiple-implementations)
  - [Complex: Controllers and Dynamic Resolution](#complex-controllers-and-dynamic-resolution)
- [Grouped Registration](#grouped-registration)
  - [Priority Ordering](#priority-ordering)
  - [Multiple Interfaces](#multiple-interfaces)
- [Generated API Reference](#generated-api-reference)
- [Compile-Time Diagnostics](#compile-time-diagnostics)
- [Agent Guidance](#agent-guidance)
- [Contributing](#contributing)
- [License](#license)

## Installation

To use Autoredi, install the main package (the source generator ships inside it) and the MEDI implementation used by your application:

```bash
dotnet add package Autoredi
dotnet add package Microsoft.Extensions.DependencyInjection
```

Autoredi references `Microsoft.Extensions.DependencyInjection.Abstractions` for its generated API. The full `Microsoft.Extensions.DependencyInjection` package supplies `ServiceCollection` and `BuildServiceProvider` in application and sample hosts.

The current Autoredi package targets .NET 10.0. For example:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

## Usage

Autoredi makes dependency injection effortless by generating DI registration code based on the `[Autoredi]` attribute. Let’s explore how to use Autoredi through a story that starts with a simple configuration service and evolves into a sophisticated notification system with controllers and dynamic service resolution.

**TryAdd semantics:** generated registrations never replace an existing descriptor, and calling a generated method twice is safe. A single implementation uses `TryAdd`; multiple implementations use `TryAddEnumerable`, so all implementations remain available through `IEnumerable<T>`. MEDI still resolves a single service using its normal last-registration behavior, so use keyed services when selection must be explicit.

### Simple: Registering a Concrete Service

Imagine you’re building a console application and need to manage basic configuration settings, like the application’s name. With Autoredi, you can register a concrete service without an interface by decorating the class with `[Autoredi]`.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Autoredi;

[Autoredi(ServiceLifetime.Singleton)]
public class AppConfig
{
    public string AppName => "MyConsoleApp";
}

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddAutorediServices(); // Generated extension method
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<AppConfig>();
        Console.WriteLine($"Application Name: {config.AppName}");
    }
}
```

**Output**:
```
Application Name: MyConsoleApp
```

Here, the `[Autoredi(ServiceLifetime.Singleton)]` attribute tells Autoredi to register `AppConfig` as a singleton. The generated `AddAutorediServices` method emits a `TryAddSingleton<AppConfig>()` registration, so you can resolve `AppConfig` directly from the service provider.

### Intermediate: Single Interface Implementation

As your application grows, you decide to add logging functionality. You define an `ILogger` interface and implement it with `ConsoleLogger`. Autoredi makes it easy to register this implementation.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Autoredi;

public interface ILogger
{
    void Log(string message);
}

[Autoredi(ServiceLifetime.Transient, typeof(ILogger))]
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[LOG]: {message}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        logger.Log("Application started successfully.");
    }
}
```

**Output**:
```
[LOG]: Application started successfully.
```

The `[Autoredi(ServiceLifetime.Transient, typeof(ILogger))]` attribute registers `ConsoleLogger` as a transient implementation of `ILogger`. Autoredi generates a `TryAdd(ServiceDescriptor.Transient<ILogger, ConsoleLogger>())` registration, allowing you to resolve `ILogger` without replacing an existing descriptor.

### Advanced: Keyed Services for Multiple Implementations

Your application now needs to send notifications via email and SMS, both implementing the same `INotificationService` interface. Autoredi supports keyed services to distinguish multiple implementations.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Autoredi;

public static class Keys
{
    public const string Email = "email";
    public const string SMS = "sms";
}

public interface INotificationService
{
    void Send(string message);
}

[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), Keys.Email)]
public class EmailNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[EMAIL]: Sending '{message}' via Email.");
    }
}

[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), Keys.SMS)]
public class SmsNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[SMS]: Sending '{message}' via SMS.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        var serviceProvider = services.BuildServiceProvider();

        var emailService = serviceProvider.GetKeyedService<INotificationService>(Keys.Email);
        var smsService = serviceProvider.GetKeyedService<INotificationService>(Keys.SMS);

        emailService.Send("Hello AOL!!");
        smsService.Send("Hello Moto!!");
    }
}
```

**Output**:
```
[EMAIL]: Sending 'Hello AOL!!' via Email.
[SMS]: Sending 'Hello Moto!!' via SMS.
```

By specifying service keys (`"email"` and `"sms"`), Autoredi emits keyed `TryAdd` registrations. Resolve them with `GetKeyedService` to select an implementation explicitly.

### Complex: Controllers and Dynamic Resolution

Now, you want to orchestrate notifications through controllers and dynamically select services at runtime. Autoredi supports advanced scenarios like keyed service injection in constructors and factory-based resolution.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Autoredi;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class Keys
{
    public const string Email = "email";
    public const string SMS = "sms";
}

public interface INotificationService
{
    void Send(string message);
}

[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), Keys.Email)]
public class EmailNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[EMAIL]: Sending '{message}' via Email.");
    }
}

[Autoredi(ServiceLifetime.Singleton, typeof(INotificationService), Keys.SMS)]
public class SmsNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[SMS]: Sending '{message}' via SMS.");
    }
}

public static class Controllers
{
    [Autoredi]
    public class MyController
    {
        private readonly INotificationService _greeting;

        public MyController([FromKeyedServices(Keys.SMS)] INotificationService greeting)
        {
            _greeting = greeting;
        }

        public void SayHello(string message)
        {
            _greeting.Send(message);
        }
    }

    [Autoredi]
    public class GreetingManager
    {
        private readonly Func<string, INotificationService> _resolver;

        public GreetingManager(Func<string, INotificationService> resolver)
        {
            _resolver = resolver;
        }

        public void Greet(string key, string name)
        {
            var service = _resolver(key) ?? throw new InvalidOperationException("Unsupported key.");
            service.Send(name);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        services.AddSingleton<Func<string, INotificationService>>(sp => key =>
            sp.GetKeyedService<INotificationService>(key));
        var serviceProvider = services.BuildServiceProvider();

        var controller = serviceProvider.GetRequiredService<Controllers.MyController>();
        controller.SayHello("Hello Controller!");

        var manager = serviceProvider.GetRequiredService<Controllers.GreetingManager>();
        manager.Greet(Keys.SMS, "Hello Manager!");
    }
}
```

**Output**:
```
[SMS]: Sending 'Hello Controller!' via SMS.
[SMS]: Sending 'Hello Manager!' via SMS.
```

In this scenario:
- `MyController` uses `[Autoredi]` to register itself and injects a keyed `INotificationService` (SMS) via `[FromKeyedServices("sms")]`.
- `GreetingManager` dynamically resolves `INotificationService` instances using a `Func<string, INotificationService>` factory, registered manually to map keys to services.
- Autoredi emits `TryAdd` registrations for `MyController` and `GreetingManager`, integrating with the keyed services without replacing existing descriptors.

This demonstrates Autoredi’s flexibility in handling complex DI scenarios, from constructor injection to runtime service selection.

### Grouped Registration

For large applications, you can organize services into named groups for selective registration. This is useful for modularizing your DI setup or conditionally registering sets of services.

```csharp
// Group: "Firebase"
[Autoredi(ServiceLifetime.Singleton, group: "Firebase")]
public class FirebaseConfig { }

[Autoredi(ServiceLifetime.Transient, group: "Firebase")]
public class FirebaseRepository { }

// Group: "Account"
[Autoredi(ServiceLifetime.Scoped, group: "Account")]
public class AccountService { }

// No Group (Default)
[Autoredi(ServiceLifetime.Transient)]
public class GlobalService { }
```

**Usage:**

```csharp
var services = new ServiceCollection();

// Option 1: Register the default (ungrouped) services for this assembly
services.AddAutorediServices();

// Option 2: Selective registration (per assembly, per group)
services.AddAutorediServicesFirebase(); // Registers only this assembly's Firebase group
services.AddAutorediServicesAccount();  // Registers only this assembly's Account group

// Option 3: Register every service emitted from this assembly
services.AddAutorediServicesSamplesModularApp();

// Option 4: Register all services from this assembly and its direct referenced assemblies that define Autoredi registrations
services.AddAutorediServicesAll();
```

`AddAutorediServices` handles the ungrouped services of this assembly, while `AddAutorediServices{AssemblyName}` registers every group that assembly contributes.

*Note: group methods are **per-assembly**. A group method generated in your app only knows the app's own registrations; a library contributes its groups through its own generated class. `AddAutorediServicesAll()` aggregates the current assembly and its direct referenced assemblies. For deeper dependency graphs, call each contributing generated class explicitly:*

```csharp
using InfrastructureAutoredi = Samples.Modular.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;

services.AddAutorediServices();                       // app defaults
InfrastructureAutoredi.AddAutorediServicesStorage(services); // one group from a library
```

See `samples/Samples.Modular.App` for a complete cross-project example.

**Group naming rules:** group names become part of the generated method name (`"Firebase"` → `AddAutorediServicesFirebase`). Names that are not valid C# identifiers are sanitized with a naming warning (`AUTOREDI018`), and names that would collide with another generated method — including `"All"` and the assembly fragment — are reported as errors (`AUTOREDI023`) and skipped. An assembly whose fragment is `All` uses the fallback `AddAutorediServicesAllAssembly` method so the cross-assembly aggregator remains callable.

### Priority Ordering

You can control the order in which services are registered within their groups using the `priority` parameter. Higher values are registered first.

```csharp
// Priority 100: Registered first
[Autoredi(ServiceLifetime.Singleton, priority: 100)]
public class FirstService { }

// Priority 50: Registered second
[Autoredi(ServiceLifetime.Singleton, priority: 50)]
public class SecondService { }

// Default Priority (0): Registered last (in alphabetical order)
[Autoredi(ServiceLifetime.Singleton)]
public class LastService { }
```

Priorities are scoped to the selected registration method. The generated descriptor order is descending priority, with alphabetical type-name tie-breaking. When multiple implementations share a service type, MEDI's normal single-service resolution still uses the last matching descriptor, so use keyed services when priority must determine the selected implementation.

### Multiple Interfaces

One attribute can register a class against several interfaces. When `InterfaceTypes` has at least one entry, it replaces `interfaceType` entirely and no self-registration is emitted:

```csharp
[Autoredi(ServiceLifetime.Scoped, InterfaceTypes = [typeof(IRepo), typeof(ICache)])]
public class RedisStore : IRepo, ICache { }
```

Generates one descriptor per unique interface. When an interface has only one implementation, Autoredi uses `TryAdd`; when multiple implementations exist anywhere in the assembly, it uses `TryAddEnumerable` consistently so every implementation remains resolvable:

```csharp
services.TryAdd(ServiceDescriptor.Scoped<IRepo, RedisStore>());
services.TryAdd(ServiceDescriptor.Scoped<ICache, RedisStore>());
```

Each entry must be an interface implemented by the decorated class; otherwise the generator reports an error at compile time instead of producing broken code. The decorated class must also be non-static, non-abstract, non-generic, and have a public constructor.

## Generated API Reference

For an assembly named `MyApp`, the generator emits:

```csharp
namespace MyApp.Autoredi;

public static partial class AutorediServiceCollectionExtensions
{
    public static IServiceCollection AddAutorediServices(this IServiceCollection services);
    public static IServiceCollection AddAutorediServices{Group}(this IServiceCollection services);   // per group
    public static IServiceCollection AddAutorediServicesMyApp(this IServiceCollection services);      // all groups of this assembly
    // Executables additionally emit:
    public static IServiceCollection AddAutorediServicesAll(this IServiceCollection services);
}
```

Every method returns the same `IServiceCollection` for chaining and carries full XML documentation (visible via IntelliSense). Remember the `using MyApp.Autoredi;`.

## Compile-Time Diagnostics

The generator validates attribute usage instead of emitting broken code:

| Id | Severity | Meaning | Fix |
|---|---|---|---|
| AUTOREDI007 | Error | Class does not implement the requested interface | Implement it or remove `interfaceType`/entry |
| AUTOREDI010 | Error | Invalid `ServiceLifetime` value | Use Singleton (0), Scoped (1), or Transient (2) |
| AUTOREDI011 | Error | Requested service type is not an interface (or null) | Pass an interface type |
| AUTOREDI012 | Error | Decorated implementation type cannot be registered by MEDI | Use a concrete, non-generic class with a public constructor |
| AUTOREDI018 | Warning | Group name contains characters that require identifier sanitization | None required; method generated from sanitized name (`"my-group"` → `AddAutorediServicesMyGroup`) |
| AUTOREDI023 | Error | Generated method name collision (`"All"` reserved, group vs assembly fragment, duplicate fragments) | Rename one side; colliding registrations are skipped until resolved |

## Agent Guidance

Autoredi ships with a coding-agent skill inside the NuGet package. After installing the package, point your agent at:

```
~/.nuget/packages/autoredi/<version>/skills/Autoredi/SKILL.md
```

It covers the attribute surface, generated-method map, TryAdd contract, diagnostics table, and common pitfalls in a form optimized for AI coding assistants (opencode/Claude Code style SKILL.md frontmatter).

## Contributing

Contributions are welcome! To get started:
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add YourFeature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a pull request.

Please include tests for new features and follow the existing coding style. Report issues or suggest enhancements via the [issue tracker](https://github.com/koddek/Autoredi/issues).

## License

Autoredi is licensed under the [MIT License](LICENSE). See the LICENSE file for details.
