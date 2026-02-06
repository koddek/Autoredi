namespace Autoredi.Generators;

/// <summary>
/// Incremental source generator for Autoredi.
/// </summary>
[Generator]
public sealed class AutorediGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.Flow()
            .ForAttributeWithMetadataName<ServiceRegistration>(Names.AutorediAttFullName)
            .Select(ServiceRegistrationExtractor.Extract)
            .Collect()
            .EmitAll((spc, registrations) =>
            {
                if (registrations.IsDefaultOrEmpty)
                {
                    return;
                }

                // The consumer projects follow [AssemblyName].Autoredi pattern.
                var targetNamespace = registrations[0].AssemblyName + ".Autoredi";

                var source = AutorediSourceBuilder.Generate(registrations, targetNamespace);
                spc.AddSource("AutorediServices.g.cs", source);
            })
            .Build()
            .Initialize(context);
    }
}
