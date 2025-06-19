# Autoredi: Compile-Time Dependency Injection Registration

Autoredi is a .NET source generator that streamlines dependency injection setup by automating service registration at compile-time. It enables you to declare your DI intent directly on your service classes using simple attributes, eliminating the need for manual `services.Add...` calls in your `Startup.cs` or `Program.cs`.

-----

## What is Autoredi?

Autoredi is built on the power of .NET Source Generators. Instead of registering your services in a runtime method (like `AddSingleton`, `AddTransient`, `AddScoped`), you simply decorate your service classes with `[Autoredi]` attributes. During compilation, Autoredi analyzes these attributes and **generates** the necessary DI registration code for you. This approach ensures your DI setup is always up-to-date with your codebase and provides compile-time feedback for common DI misconfigurations.

-----

## Features

* **Zero Manual Registration:** No more repetitive `services.Add...` lines.
* **Compile-Time Safety:** Catches common DI issues (like missing interface implementations) at build time, not runtime.
* **Clear Intent:** Declare DI lifetimes and interface relationships directly on your classes.
* **Single Attribute, Clear Intent:**
    * `[Autoredi(lifetime)]`: Registers a concrete service directly (e.g., `MyService`). Useful for services without interfaces or direct concrete resolutions.
    * `[Autoredi<TInterface>(alias, lifetime)]`: Registers a service by its interface. The `alias` parameter is optional for single implementations and **required** when an interface has multiple concrete implementations.
* **Aliased Registrations:** Easily register multiple concrete services for the same interface using aliases, making resolution explicit and manageable.
* **Intelligent Warnings:** Provides helpful compiler warnings if an interface with multiple implementations lacks an alias, guiding you to best practices.

-----

## Installation

1.  **Add the NuGet Package:**
    Install the `Autoredi.Generators` NuGet package to your project (the project containing your service classes).

    ```bash
    dotnet add package Autoredi
    ```

    *(Ensure you also have `Autoredi.Attributes` in a shared project or directly referenced if split.)*

-----

## Usage Example

### 1\. Define Services and Interfaces

Decorate your service classes with `Autoredi` attributes.

```csharp
// Define your interfaces
namespace MyConsoleApp.Services;

public interface ILogger
{
    void Log(string message);
}

public interface INotificationService
{
    void Send(string message);
}

// Implementations using Autoredi attributes
using Autoredi.Attributes; // Important!

// 1. Service without an interface (registers AppConfig directly)
[Autoredi(ServiceLifetime.Singleton)]
public class AppConfig
{
    public string AppName => "MyConsoleApp";
}

// 2. Single implementation of an interface (registers ILogger -> ConsoleLogger)
[Autoredi<ILogger>(ServiceLifetime.Transient)]
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[LOG]: {message}");
    }
}

// 3. Multiple implementations of an interface, using aliases (registers INotificationService via dictionary)
[Autoredi<INotificationService>("Email", ServiceLifetime.Singleton)]
public class EmailNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[EMAIL]: Sending '{message}' via Email.");
    }
}

[Autoredi<INotificationService>("SMS", ServiceLifetime.Singleton)]
public class SmsNotificationService : INotificationService
{
    public void Send(string message)
    {
        Console.WriteLine($"[SMS]: Sending '{message}' via SMS.");
    }
}
```

### 2\. Register and Use Services

In your `Program.cs` (for a .NET 6+ console app with top-level statements), use the generated extension method:

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using MyConsoleApp.Services;
using Autoredi.Generated; // This is crucial for the generated extension method
using System.Collections.Generic; // Required for IDictionary resolution

// 1. Create a ServiceCollection
var services = new ServiceCollection();

// 2. Register Autoredi services using the generated extension method
// This call will include all services decorated with [Autoredi] attributes.
services.AddAutorediServices();

// 3. Build the ServiceProvider
var serviceProvider = services.BuildServiceProvider();

// 4. Resolve and use your services

// Resolve a service registered without an interface
var config = serviceProvider.GetRequiredService<AppConfig>();
Console.WriteLine($"Application Name: {config.AppName}");

// Resolve a service registered with a single interface implementation
var logger = serviceProvider.GetRequiredService<ILogger>();
logger.Log("Application started.");

// Resolve aliased services using IDictionary<string, TInterface>
var notificationServices = serviceProvider.GetRequiredService<IDictionary<string, INotificationService>>();

var emailService = notificationServices["Email"];
emailService.Send("Welcome to Autoredi!");

var smsService = notificationServices["SMS"];
smsService.Send("Your order has been shipped.");

Console.WriteLine("Services resolved and used successfully!");
```

-----

## Diagnostics and Warnings

Autoredi provides custom compiler diagnostics to guide you:

* **`AUTOREDI001` (Warning): Interface Requires Alias**
    * Triggered when an interface is implemented by multiple concrete services, and one or more of those implementations are missing an `alias` in their `Autoredi<TInterface>` attribute. All implementations for such an interface must have an alias.
* **`AUTOREDI002` (Warning): Alias on Non-Generic Attribute**
    * Triggered if you try to specify an `alias` on the non-generic `[Autoredi]` attribute. Aliases are only applicable to `[Autoredi<TInterface>]` for interface-based registrations.
* **`AUTOREDI003` (Error): Generic AutorediAttribute Missing Interface Implementation**
    * Triggered if a class is decorated with `[Autoredi<TInterface>]` but does not actually implement `TInterface`. This is an error because it would lead to runtime failures.
* **`AUTOREDI004` (Error): Multiple Autoredi Attributes on Class**
    * Triggered if a class has more than one `Autoredi` attribute applied (e.g., `[Autoredi]` and `[Autoredi<TInterface>]` on the same class). Only one `Autoredi` attribute (of any type) is allowed per class.

-----

## Contribution

Contributions are welcome\! Please feel free to open issues or submit pull requests.

-----

## License

This project is licensed under the [MIT License](LICENSE.md).

-----
