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
        services.AddScoped<IAdPatchRepository, MongoAdPatchRepository>();
        services.AddScoped<IAdPatchCommandService, AdPatchCommandService>();
        services.AddScoped<IAdPatchQueryService, AdPatchQueryService>();
        services.AddScoped<IOlxAdministratorAdPatchSearchService, OlxAdministratorAdPatchSearchService>();
        services.AddScoped<IOlxOperatorAdPatchSearchService, OlxOperatorAdPatchSearchService>();
        services.AddScoped<IOlxOperator, OlxOperator>();
        services.AddScoped<IOlxAdministrator, OlxAdministrator>();
        services.AddScoped<IVictim, Victim>();
        return services;
    }
}
