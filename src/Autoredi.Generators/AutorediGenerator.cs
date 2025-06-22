using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Autoredi.Generators.Diagnostics;
using static Autoredi.Generators.Names;

namespace Autoredi.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class AutorediGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Launch();
        }
#endif
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateTypeDeclaration(node),
                transform: static (gsc, _) => (ClassDeclarationSyntax)gsc.Node);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsCandidateTypeDeclaration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
        {
            return false;
        }

        if (classDecl.AttributeLists.Count == 0)
        {
            return false;
        }

        return classDecl.AttributeLists
            .Any(attrList => attrList.Attributes
                .Select(attr => attr.Name.ToString())
                .Any(name => name == AutorediAttName ||
                             name == AutorediAttNameWithPostfix ||
                             name.StartsWith(AutorediAttName + "<")));
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        // Check 1: Ensure classes are provided
        if (classes.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(NoClassesFoundDescriptor, null));
            return;
        }

        // Check 2: Get the ServiceLifetime type symbol
        var serviceLifetimeType =
            compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceLifetime");
        if (serviceLifetimeType == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingServiceLifetimeTypeDescriptor, null));
            return;
        }

        // Check 3: Verify ServiceLifetime is an enum
        if (serviceLifetimeType.TypeKind != TypeKind.Enum)
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidServiceLifetimeTypeDescriptor, null));
            return;
        }

        // Check 4: Get valid ServiceLifetime values
        var validLifetimeValues = serviceLifetimeType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .Select(f => (int)f.ConstantValue!)
            .ToImmutableHashSet();

        if (validLifetimeValues.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(NoValidLifetimeValuesDescriptor, null));
            return;
        }

        // Check 5: Get the attribute type symbol
        var attributeType = compilation.GetTypeByMetadataName(AutorediAttFullName);
        if (attributeType == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingAttributeTypeDescriptor, null));
            return;
        }

        // Determine target namespace from project name
        var projectName = compilation.AssemblyName ?? "Autoredi.Generated";
        var targetNamespace = $"{projectName}.Autoredi";

        // Collect referenced assemblies with Autoredi attribute or namespace
        var visitedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { projectName };
        var referencedAutorediNamespaces =
            GetReferencedAutorediNamespaces(compilation, projectName, attributeType, visitedAssemblies, context);

        var registrations = new List<RegistrationInfo>();

        foreach (var classDecl in classes)
        {
            // Check 6: Get class symbol
            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as ITypeSymbol;
            if (classSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidClassSymbolDescriptor, classDecl.GetLocation(),
                    classDecl.Identifier.Text));
                continue;
            }

            // Check 7: Find AutorediAttribute
            var attributeData = classSymbol.GetAttributes()
                .FirstOrDefault(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeType));
            if (attributeData == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingAttributeOnClassDescriptor, classDecl.GetLocation(),
                    classSymbol.Name));
                continue;
            }

            // Check 8: Validate constructor arguments
            if (attributeData.ConstructorArguments.Length != 3)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidAttributeArgumentsDescriptor, classDecl.GetLocation(),
                    classSymbol.Name));
                continue;
            }

            var lifetimeArg = attributeData.ConstructorArguments[0];
            var interfaceTypeArg = attributeData.ConstructorArguments[1];
            var serviceKeyArg = attributeData.ConstructorArguments[2];

            // Check 9: Validate lifetime argument type
            if (lifetimeArg.Value is not int lifetimeInt)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidLifetimeTypeDescriptor, classDecl.GetLocation(),
                    classSymbol.Name));
                continue;
            }

            // Check 10: Validate lifetime value
            if (!validLifetimeValues.Contains(lifetimeInt))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidLifetimeDescriptor, classDecl.GetLocation(),
                    classSymbol.Name));
                continue;
            }

            // Check 11: Validate interface type
            string? interfaceName = null;
            if (interfaceTypeArg is { IsNull: false, Value: ITypeSymbol interfaceSymbol })
            {
                if (interfaceSymbol.TypeKind != TypeKind.Interface)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidInterfaceTypeDescriptor, classDecl.GetLocation(),
                        classSymbol.Name, interfaceSymbol.Name));
                    continue;
                }

                interfaceName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // Check 12: Validate service key type
            string? serviceKey = null;
            if (!serviceKeyArg.IsNull)
            {
                if (serviceKeyArg.Value is not string keyValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidServiceKeyTypeDescriptor, classDecl.GetLocation(),
                        classSymbol.Name));
                    continue;
                }

                serviceKey = keyValue;
            }

            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            registrations.Add(new RegistrationInfo
            {
                ClassName = className,
                Lifetime = lifetimeInt,
                InterfaceName = interfaceName,
                ServiceKey = serviceKey,
                Location = classDecl.GetLocation()
            });
        }

        // Check 13: Validate for duplicate class registrations
        var duplicateClasses = registrations.GroupBy(r => r.ClassName)
            .Where(g => g.Count() > 1)
            .Select(g => (ClassName: g.Key, Locations: g.Select(r => r.Location).ToList()))
            .ToList();

        foreach (var (className, locations) in duplicateClasses)
        {
            foreach (var location in locations)
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateClassDescriptor, location, className));
            }
        }

        // Check 14: Validate non-keyed registrations
        var registrationsWithoutKey = registrations.Where(r => r.ServiceKey == null).ToList();
        var duplicateServiceTypesWithoutKey = registrationsWithoutKey
            .GroupBy(r => r.InterfaceName ?? r.ClassName)
            .Where(g => g.Count() > 1)
            .Select(g => (ServiceType: g.Key, Locations: g.Select(r => r.Location).ToList()))
            .ToList();

        foreach (var (serviceType, locations) in duplicateServiceTypesWithoutKey)
        {
            foreach (var location in locations)
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateRegistrationWithoutKeyDescriptor, location,
                    serviceType));
            }
        }

        // Check 15: Validate keyed registrations
        var registrationsWithKey = registrations.Where(r => r.ServiceKey != null).ToList();
        var duplicateKeyedRegistrations = registrationsWithKey
            .GroupBy(r => (ServiceType: r.InterfaceName ?? r.ClassName, r.ServiceKey))
            .Where(g => g.Count() > 1)
            .Select(g => (g.Key.ServiceType, g.Key.ServiceKey, Locations: g.Select(r => r.Location).ToList()))
            .ToList();

        foreach (var (serviceType, key, locations) in duplicateKeyedRegistrations)
        {
            foreach (var location in locations)
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateRegistrationWithKeyDescriptor, location,
                    serviceType));
            }
        }

        // Check 16: Validate interfaces with multiple implementations
        var interfaceUsage = registrations
            .Where(r => r.InterfaceName != null)
            .GroupBy(r => r.InterfaceName)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in interfaceUsage)
        {
            var missingKeyRegistrations = group.Where(r => r.ServiceKey == null).ToList();
            foreach (var reg in missingKeyRegistrations)
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingAliasForMultipleInterfaceDescriptor, reg.Location,
                    group.Key));
            }
        }

        // Stop if any validation errors exist
        if (duplicateClasses.Any() || duplicateServiceTypesWithoutKey.Any() ||
            duplicateKeyedRegistrations.Any() || interfaceUsage.Any(g => g.Any(r => r.ServiceKey == null)))
        {
            return;
        }

        // Check 17: Verify IServiceCollection exists for code generation
        var serviceCollectionType =
            compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        if (serviceCollectionType == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingServiceCollectionTypeDescriptor, null));
            return;
        }

        // Sort registrations
        var sortedRegistrations = registrations
            .Where(r => r.InterfaceName == null)
            .OrderBy(r => r.ClassName)
            .Concat(
                registrations
                    .Where(r => r.InterfaceName != null)
                    .OrderBy(r => r.InterfaceName)
                    .ThenBy(r => r.ClassName))
            .ToList();

        // Generate the extension method
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine($"namespace {targetNamespace};");
        sb.AppendLine();
        sb.AppendLine("public static class AutorediExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddAutorediServices(this IServiceCollection services)");
        sb.AppendLine("    {");

        // Call AddAutorediServices for referenced projects using fully qualified names
        foreach (var ns in referencedAutorediNamespaces)
        {
            var assemblyName = ns.Substring(0, ns.Length - ".Autoredi".Length);
            sb.AppendLine($"        {ns}.AutorediExtensions.AddAutorediServices(services); // {assemblyName}");
        }

        if (!referencedAutorediNamespaces.IsEmpty)
        {
            sb.AppendLine();
        }

        foreach (var reg in sortedRegistrations)
        {
            var lifetimeMethod = reg.Lifetime switch
            {
                0 => "Singleton",
                1 => "Scoped",
                2 => "Transient",
                _ => throw new InvalidOperationException("Invalid lifetime") // Unreachable
            };

            if (reg.ServiceKey == null)
            {
                if (reg.InterfaceName != null)
                {
                    sb.AppendLine($"        services.Add{lifetimeMethod}<{reg.InterfaceName}, {reg.ClassName}>();");
                }
                else
                {
                    sb.AppendLine($"        services.Add{lifetimeMethod}<{reg.ClassName}>();");
                }
            }
            else
            {
                if (reg.InterfaceName != null)
                {
                    sb.AppendLine(
                        $"        services.AddKeyed{lifetimeMethod}<{reg.InterfaceName}, {reg.ClassName}>(\"{reg.ServiceKey}\");");
                }
                else
                {
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}<{reg.ClassName}>(\"{reg.ServiceKey}\");");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var fileName = $"{targetNamespace}.AutorediExtensions.cs";
        context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static ImmutableArray<string> GetReferencedAutorediNamespaces(
        Compilation compilation,
        string projectName,
        INamedTypeSymbol attributeType,
        HashSet<string> visitedAssemblies,
        SourceProductionContext context)
    {
        var namespaces = new List<string>();

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm ||
                asm.Name == projectName ||
                string.IsNullOrEmpty(asm.Name) ||
                asm.Name.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                asm.Name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                asm.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                asm.Name.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip if already visited to prevent circular dependencies
            if (!visitedAssemblies.Add(asm.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "AUTOREDI002",
                        "Circular Dependency Detected",
                        $"Circular reference detected for assembly '{asm.Name}'. Skipping to prevent infinite recursion.",
                        "Autoredi",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    null));
                continue;
            }

            // Check for Autoredi attribute or namespace
            if (HasAutorediAttributeOrNamespace(compilation, asm, attributeType))
            {
                namespaces.Add($"{asm.Name}.Autoredi");
            }

            visitedAssemblies.Remove(asm.Name); // Allow re-visiting in other contexts
        }

        return namespaces.Distinct().ToImmutableArray();
    }

    private static bool HasAutorediAttributeOrNamespace(Compilation compilation, IAssemblySymbol assembly,
        INamedTypeSymbol attributeType)
    {
        // Check for Autoredi namespace (e.g., <AssemblyName>.Autoredi)
        if (compilation.GetTypeByMetadataName($"{assembly.Name}.Autoredi.AutorediExtensions") != null)
        {
            return true;
        }

        // Check for Autoredi attribute on any type
        var types = GetAllTypes(assembly.GlobalNamespace);
        return types.Any(type => type.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)));
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    private sealed record RegistrationInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public int Lifetime { get; set; }
        public string? InterfaceName { get; set; }
        public string? ServiceKey { get; set; }
        public Location? Location { get; set; }
    }
}
