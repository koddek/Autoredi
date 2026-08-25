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

To use Autoredi, install the main package (the source generator ships inside it):

```bash
dotnet add package Autoredi
```

Your project should target .NET 10.0 or a compatible framework. For example:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

## Usage

Autoredi makes dependency injection effortless by generating DI registration code based on the `[Autoredi]` attribute. Let’s explore how to use Autoredi through a story that starts with a simple configuration service and evolves into a sophisticated notification system with controllers and dynamic service resolution.

**TryAdd semantics:** all generated registrations fill gaps and never override services you registered manually before calling them. Calling a generated method twice is safe and adds no duplicates. Single-implementation service types emit `services.TryAdd(ServiceDescriptor.*)`; interfaces with multiple implementations emit `services.TryAddEnumerable(ServiceDescriptor.*)` so every implementation stays resolvable via `IEnumerable<T>`.

### Simple: Registering a Concrete Service

Imagine you’re building a console application and need to manage basic configuration settings, like the application’s name. With Autoredi, you can register a concrete service without an interface by decorating the class with `[Autoredi]`.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

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

Here, the `[Autoredi(ServiceLifetime.Singleton)]` attribute tells Autoredi to register `AppConfig` as a singleton. The generated `AddAutorediServices` method handles the registration (`services.AddSingleton<AppConfig>()`), so you can resolve `AppConfig` directly from the service provider. No manual DI setup required!

### Intermediate: Single Interface Implementation

As your application grows, you decide to add logging functionality. You define an `ILogger` interface and implement it with `ConsoleLogger`. Autoredi makes it easy to register this implementation.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

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

The `[Autoredi(ServiceLifetime.Transient, typeof(ILogger))]` attribute registers `ConsoleLogger` as a transient implementation of `ILogger`. Autoredi generates `services.AddTransient<ILogger, ConsoleLogger>()`, allowing you to resolve `ILogger` seamlessly.

### Advanced: Keyed Services for Multiple Implementations

Your application now needs to send notifications via email and SMS, both implementing the same `INotificationService` interface. Autoredi supports keyed services to distinguish multiple implementations.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

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

By specifying service keys (`"email"` and `"sms"`), Autoredi registers `EmailNotificationService` and `SmsNotificationService` as keyed services (`services.AddKeyedSingleton<INotificationService, EmailNotificationService>("email")`, etc.). You resolve them using `GetKeyedService`, enabling precise control over which implementation to use.

### Complex: Controllers and Dynamic Resolution

Now, you want to orchestrate notifications through controllers and dynamically select services at runtime. Autoredi supports advanced scenarios like keyed service injection in constructors and factory-based resolution.

```csharp
using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
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
- Autoredi generates registrations for `MyController` and `GreetingManager` (`services.AddTransient<MyController>()`, etc.), integrating seamlessly with the keyed services.

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

// Option 4: Register all services from this assembly and referenced assemblies that define Autoredi registrations
services.AddAutorediServicesAll();
```

`AddAutorediServices` handles the ungrouped services of this assembly, while `AddAutorediServices{AssemblyName}` registers every group that assembly contributes.

*Note: group methods are **per-assembly**. A group method generated in your app only knows the app's own registrations; a library contributes its groups through its own generated class. For cross-assembly registration use `AddAutorediServicesAll()`, or call the referenced assembly's class directly:*

```csharp
using InfrastructureAutoredi = Samples.Modular.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;

services.AddAutorediServices();                       // app defaults
InfrastructureAutoredi.AddAutorediServicesStorage(services); // one group from a library
```

See `samples/Samples.Modular.App` for a complete cross-project example.

**Group naming rules:** group names become part of the generated method name (`"Firebase"` → `AddAutorediServicesFirebase`). Names that are not valid C# identifiers are sanitized with a naming warning (`AUTOREDI018`), and names that would collide with another generated method — including `"All"` and the assembly fragment — are reported as errors (`AUTOREDI023`) and skipped.

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

Priorities are scoped to their group (or the default group). Because registrations use TryAdd semantics, order decides who wins when several services target the same service type: the first registration for a given (service type, implementation) pair sticks, so priority is how you choose which implementation fills the gap first when nothing was registered manually.

### Multiple Interfaces

One attribute can register a class against several interfaces. When `InterfaceTypes` has at least one entry, it replaces `interfaceType` entirely and no self-registration is emitted:

```csharp
[Autoredi(ServiceLifetime.Scoped, InterfaceTypes = [typeof(IRepo), typeof(ICache)])]
public class RedisStore : IRepo, ICache { }
```

Generates one descriptor per interface. When RedisStore is the only implementation of each interface, the descriptors use plain `TryAdd`; a second class implementing either interface flips that interface's registrations to `TryAddEnumerable` so both stay resolvable:

```csharp
services.TryAdd(ServiceDescriptor.Scoped<IRepo, RedisStore>());
services.TryAdd(ServiceDescriptor.Scoped<ICache, RedisStore>());
```

Each entry must be an interface implemented by the decorated class; otherwise the generator reports an error at compile time instead of producing broken code.

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
| AUTOREDI010 | Error | Invalid `ServiceLifetime` value | Use Transient (0), Scoped (1), or Singleton (2) |
| AUTOREDI011 | Error | Requested service type is not an interface (or null) | Pass an interface type |
| AUTOREDI018 | Warning | Group name is not a valid C# identifier | None required; method generated from sanitized name (`"my-group"` → `AddAutorediServicesMyGroup`) |
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
