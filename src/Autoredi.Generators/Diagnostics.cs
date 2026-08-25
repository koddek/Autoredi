using Microsoft.CodeAnalysis;

namespace Autoredi.Generators;

/// <summary>
/// Diagnostics reported by the Autoredi generator. Only descriptors that are actually
/// wired to a code path live here.
/// </summary>
public static class Diagnostics
{
    private const string Category = "AutorediSourceGenerator";

    public static readonly DiagnosticDescriptor InterfaceNotImplemented = new(
        id: "AUTOREDI007",
        title: "Interface not implemented",
        messageFormat: "Class '{0}' is decorated with [Autoredi] but does not implement interface '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidLifetime = new(
        id: "AUTOREDI010",
        title: "Invalid service lifetime specified",
        messageFormat: "The service lifetime specified for class '{0}' is invalid. Use a valid ServiceLifetime value (Transient, Scoped, or Singleton).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidInterfaceType = new(
        id: "AUTOREDI011",
        title: "Invalid interface type",
        messageFormat: "The type '{1}' specified for class '{0}' is not an interface (or is null). Specify a valid interface type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidGroupName = new(
        id: "AUTOREDI018",
        title: "Invalid group name",
        messageFormat: "The group name '{0}' is not a valid C# identifier. The generated method will be named 'AddAutorediServices{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNameCollision = new(
        id: "AUTOREDI023",
        title: "Generated method name collision",
        messageFormat: "The generated method name 'AddAutorediServices{0}' collides with another generated method. Rename the group (or assembly) so each generated method name is unique; the colliding registrations are skipped.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
