using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Application.Services;
using Nexus.StrawMen.Infrastructure.Persistance;

namespace Nexus.StrawMen.Composition;

public static class StrawMenServiceCollectionExtensions
{
    public static IServiceCollection AddNexusStrawMen(this IServiceCollection services)
    {
        services.AddScoped<IStrawManAccessPolicy, StrawManAccessPolicy>();
        services.AddScoped<IStrawManSettingsRepository, MongoStrawManSettingsRepository>();
        services.AddScoped<IStrawManSettingsQueryService, StrawManSettingsQueryService>();
        services.AddScoped<IStrawManSettingsCommandService, StrawManSettingsCommandService>();
        services.AddScoped<IStrawMan, StrawMan>();
        services.AddNexusStrawMenAdministrator();

        return services;
    }
}
