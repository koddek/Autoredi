using Microsoft.CodeAnalysis;

namespace Autoredi.Generators;

/// <summary>
/// Contains all diagnostic descriptors used by the generator.
/// </summary>
public static class Diagnostics
{
    public static readonly DiagnosticDescriptor InterfaceNeedsAliasWarning = new(
        id: "AUTOREDI001",
        title: "Interface Requires Alias",
        messageFormat:
        "Interface '{0}' is implemented by multiple services. All registrations for this interface must use an alias. Consider adding an alias like `[Autoredi<{0}>(alias: \"{1}\")]`.",
        category: "AutoRedi",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AliasOnNonGenericAttributeWarning = new(
        id: "AUTOREDI002",
        title: "Alias on Non-Generic Attribute",
        messageFormat:
        "Class '{0}' is decorated with a non-generic AutorediAttribute that specifies an alias. Aliases are only applicable to generic AutorediAttribute<TInterface> types. The alias will be ignored.",
        category: "AutoRedi",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    public static readonly DiagnosticDescriptor InvalidClassTypeDescriptor = new(
        id: "AUTOREDI006",
        title: "Invalid class type for registration",
        messageFormat: "The type '{0}' cannot be registered because it is abstract or an interface.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    public static readonly DiagnosticDescriptor NoInterfaceWarning = new(
        id: "AUTOREDI007",
        title: "No interface specified",
        messageFormat: "The class '{0}' is registered without an interface. Consider specifying an interface for better decoupling.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingInterfaceForGenericAttributeError = new(
        id: "AUTOREDI003",
        title: "Generic AutorediAttribute Missing Interface Implementation",
        messageFormat: "Class '{0}' is decorated with `AutorediAttribute<{1}>` but does not implement interface '{1}'.",
        category: "AutoRedi",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleAutorediAttributesError = new(
        id: "AUTOREDI004",
        title: "Multiple Autoredi Attributes on Class",
        messageFormat:
        "Class '{0}' has multiple Autoredi attributes. Only one Autoredi attribute (of any type) is allowed per class. Remove extra attributes.",
        category: "AutoRedi",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleImplementationsWithoutKey = new(
        "AUT001",
        "Multiple implementations without service key",
        "Multiple implementations for service type '{0}' must all have unique service keys.",
        "Autoredi",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateServiceKey = new(
        "AUT002",
        "Duplicate service key",
        "Service key '{1}' is used multiple times for service type '{0}'.",
        "Autoredi",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateClassRegistration = new(
        id: "AUTOREDI001",
        title: "Duplicate class registration",
        messageFormat: "Class '{0}' has multiple Autoredi attributes",
        category: "AutorediGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleRegistrationsWithoutKey = new(
        id: "AUTOREDI002",
        title: "Multiple interface implementations require keys",
        messageFormat: "Interface '{0}' has multiple implementations without service keys",
        category: "AutorediGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    //=======

    private const string Category = "AutorediSourceGenerator";

    public static readonly DiagnosticDescriptor NoClassesFoundDescriptor = new(
        id: "AUTOREDI001",
        title: "No classes found with AutorediAttribute",
        messageFormat: "No classes with the AutorediAttribute were found. Ensure classes are decorated with [Autoredi].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingServiceLifetimeTypeDescriptor = new(
        id: "AUTOREDI002",
        title: "Missing ServiceLifetime type",
        messageFormat: "The Microsoft.Extensions.DependencyInjection.ServiceLifetime enum was not found. Ensure the project references Microsoft.Extensions.DependencyInjection.Abstractions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidServiceLifetimeTypeDescriptor = new(
        id: "AUTOREDI003",
        title: "Invalid ServiceLifetime type",
        messageFormat: "The ServiceLifetime type is not an enum. Ensure the correct Microsoft.Extensions.DependencyInjection.Abstractions assembly is referenced.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoValidLifetimeValuesDescriptor = new(
        id: "AUTOREDI004",
        title: "No valid ServiceLifetime values",
        messageFormat: "No valid values were found in the ServiceLifetime enum. Ensure the Microsoft.Extensions.DependencyInjection.Abstractions assembly is correct.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingAttributeTypeDescriptor = new(
        id: "AUTOREDI005",
        title: "Missing AutorediAttribute type",
        messageFormat: "The AutorediAttribute type was not found. Ensure the attribute is defined and accessible.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidClassSymbolDescriptor = new(
        id: "AUTOREDI006",
        title: "Invalid class symbol",
        messageFormat: "The class '{0}' could not be resolved to a valid symbol. Ensure it is correctly defined.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingAttributeOnClassDescriptor = new(
        id: "AUTOREDI007",
        title: "Missing AutorediAttribute on class",
        messageFormat: "The class '{0}' was identified as a candidate but lacks the AutorediAttribute.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAttributeArgumentsDescriptor = new(
        id: "AUTOREDI008",
        title: "Invalid AutorediAttribute arguments",
        messageFormat: "The AutorediAttribute on class '{0}' has an invalid number of arguments. Expected: lifetime, interfaceType, serviceKey.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidLifetimeTypeDescriptor = new(
        id: "AUTOREDI009",
        title: "Invalid lifetime argument type",
        messageFormat: "The lifetime argument for class '{0}' is not an integer. It must be a valid ServiceLifetime value.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidLifetimeDescriptor = new(
        id: "AUTOREDI010",
        title: "Invalid service lifetime specified",
        messageFormat: "The service lifetime specified for class '{0}' is invalid. Use a valid ServiceLifetime value (Transient, Scoped, or Singleton).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidInterfaceTypeDescriptor = new(
        id: "AUTOREDI011",
        title: "Invalid interface type",
        messageFormat: "The interface type specified for class '{0}' ('{1}') is not an interface. Specify a valid interface type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidServiceKeyTypeDescriptor = new(
        id: "AUTOREDI012",
        title: "Invalid service key type",
        messageFormat: "The service key for class '{0}' is not a string. It must be a string or null.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateClassDescriptor = new(
        id: "AUTOREDI013",
        title: "Duplicate class registration",
        messageFormat: "The class '{0}' is registered multiple times with the AutorediAttribute. Each class can only be registered once.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateRegistrationWithoutKeyDescriptor = new(
        id: "AUTOREDI014",
        title: "Duplicate registration without service key",
        messageFormat: "The service type '{0}' is registered multiple times without a service key. Use a unique service key for each registration of the same service type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateRegistrationWithKeyDescriptor = new(
        id: "AUTOREDI015",
        title: "Duplicate registration with same service key",
        messageFormat: "The service type '{0}' is registered multiple times with the same service key '{1}'. Each (service type, key) combination must be unique.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingAliasForMultipleInterfaceDescriptor = new(
        id: "AUTOREDI016",
        title: "Missing service key for multiple interface implementations",
        messageFormat: "The interface '{0}' is implemented by multiple classes, but at least one registration lacks a service key. All registrations for the same interface must specify a unique service key.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingServiceCollectionTypeDescriptor = new(
        id: "AUTOREDI017",
        title: "Missing IServiceCollection type",
        messageFormat: "The Microsoft.Extensions.DependencyInjection.IServiceCollection type was not found. Ensure the project references Microsoft.Extensions.DependencyInjection.Abstractions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
