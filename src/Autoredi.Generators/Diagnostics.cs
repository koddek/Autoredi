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
        title: "Group name requires sanitization",
        messageFormat: "The group name '{0}' contains characters that are not valid in the generated method name. The generated method will be named 'AddAutorediServices{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidImplementationType = new(
        id: "AUTOREDI012",
        title: "Invalid Autoredi implementation type",
        messageFormat: "The type '{0}' cannot be registered by Autoredi. Use an accessible, non-static, non-abstract, non-generic class with a public constructor.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ServiceTypeNotAccessible = new(
        id: "AUTOREDI024",
        title: "Service type is not accessible from generated code",
        messageFormat: "The interface '{1}' specified for class '{0}' cannot be referenced from generated code. Use a public or internal interface that is not file-local, not private, and not nested inside a private or file-local type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReferencedAssemblyNotCompatible = new(
        id: "AUTOREDI025",
        title: "Referenced Autoredi assembly is not usable from AddAutorediServicesAll",
        messageFormat: "The referenced assembly '{0}' exposes a type named 'Autoredi.AutorediServiceCollectionExtensions' but no generated AddAutorediServices{1}(IServiceCollection) method. That method is skipped; call the assembly's own extension methods directly, or upgrade it to a version of Autoredi that emits the assembly-wide registration method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OpenGenericServiceTypeNotSupported = new(
        id: "AUTOREDI026",
        title: "Open generic service type is not supported",
        messageFormat: "The open generic type '{1}' specified for class '{0}' is not supported. Register a closed generic interface or a non-generic interface instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNameCollision = new(
        id: "AUTOREDI023",
        title: "Generated method name collision",
        messageFormat: "The generated method name 'AddAutorediServices{0}' collides with another generated method. Rename the group (or assembly) so each generated method name is unique; the colliding registrations are skipped.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
