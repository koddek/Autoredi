using Microsoft.Extensions.DependencyInjection;
using AppAutoredi = Samples.Modular.App.Autoredi.AutorediServiceCollectionExtensions;
using InfrastructureAutoredi = Samples.Modular.Infrastructure.Autoredi.AutorediServiceCollectionExtensions;

namespace Samples.Modular.App;

public static class AutorediAggregation
{
    public static IServiceCollection AddAutorediServicesAll(this IServiceCollection services)
    {
        AppAutoredi.AddAutorediServices(services);
        InfrastructureAutoredi.AddAutorediServices(services);

        return services;
    }
}
