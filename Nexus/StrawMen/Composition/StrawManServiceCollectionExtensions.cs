using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Application.Services;
using Nexus.StrawMen.Infrastructure.Persistance;

namespace Nexus.StrawMen.Composition;

public static class StrawManServiceCollectionExtensions
{
    public static IServiceCollection AddNexusStrawMan(this IServiceCollection services)
    {
        services.AddScoped<IStrawManAccessPolicy, StrawManAccessPolicy>();
        services.AddScoped<IStrawManPaymentSearchService, StrawManPaymentSearchService>();
        services.AddScoped<IStrawManSettingsRepository, MongoStrawManSettingsRepository>();
        services.AddScoped<IStrawManSettingsQueryService, StrawManSettingsQueryService>();
        services.AddScoped<IStrawManSettingsCommandService, StrawManSettingsCommandService>();
        services.AddScoped<IStrawMan, StrawMan>();

        return services;
    }
}
