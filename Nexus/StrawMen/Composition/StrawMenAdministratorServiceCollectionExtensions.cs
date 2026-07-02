using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Application.Services;

namespace Nexus.StrawMen.Composition;

public static class StrawMenAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusStrawMenAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorStrawManSettingsCommandService, AdministratorStrawManSettingsCommandService>();
        services.AddScoped<IAdministratorStrawManSettingsQueryService, AdministratorStrawManSettingsQueryService>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
