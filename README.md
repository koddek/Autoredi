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
- [Contributing](#contributing)
- [License](#license)

## Installation

To use Autoredi, install the NuGet packages for the main library and the source generator in your .NET project:

```bash
dotnet add package Autoredi --version 1.0.0
```

Ensure your project references `Microsoft.Extensions.DependencyInjection.Abstractions` (version 9.* or compatible):

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
```

Your project should target .NET 9.0 or a compatible framework. For example:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
</PropertyGroup>
```

## Usage

Autoredi makes dependency injection effortless by generating DI registration code based on the `[Autoredi]` attribute. Let’s explore how to use Autoredi through a story that starts with a simple configuration service and evolves into a sophisticated notification system with controllers and dynamic service resolution.

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

// Option 1: Register EVERYTHING (Default behavior)
services.AddAutorediServices();

// Option 2: Selective Registration
services.AddAutorediServicesFirebase(); // Registers only Firebase group
services.AddAutorediServicesAccount();  // Registers only Account group
services.AddAutorediServicesDefault();  // Registers ungrouped services
```

*Note: Group registration methods (e.g., `AddAutorediServicesFirebase`) automatically include services from the same group in referenced assemblies. Check the `samples/Samples.Modular` directory for a complete cross-project example.*

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

Priorities are scoped to their group (or the default group). This is helpful when service registration order matters, such as when using decorators or the `TryAdd` pattern (though Autoredi always uses `Add`).

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
