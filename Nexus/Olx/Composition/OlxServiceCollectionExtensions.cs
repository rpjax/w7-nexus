using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Services;
using Nexus.Olx.Infrastructure.Persistance;

namespace Nexus.Olx.Composition;

public static class OlxServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOlx(this IServiceCollection services)
    {
        services.AddScoped<IOlxOperatorAccessPolicy, OlxOperatorAccessPolicy>();
        services.AddScoped<IOlxAdministratorAccessPolicy, OlxAdministratorAccessPolicy>();
        services.AddScoped<IAdSpoofRepository, MongoAdSpoofRepository>();
        services.AddScoped<IAdSpoofCommandService, AdSpoofCommandService>();
        services.AddScoped<IAdSpoofQueryService, AdSpoofQueryService>();
        services.AddScoped<IOlxAdministratorAdSpoofSearchService, OlxAdministratorAdSpoofSearchService>();
        services.AddScoped<IOlxOperatorAdSpoofSearchService, OlxOperatorAdSpoofSearchService>();
        services.AddScoped<IOlxOperator, OlxOperator>();
        services.AddScoped<IOlxAdministrator, OlxAdministrator>();
        services.AddScoped<IVictim, Victim>();
        return services;
    }
}
