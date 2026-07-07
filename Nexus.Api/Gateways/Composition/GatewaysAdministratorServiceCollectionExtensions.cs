using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Services;

namespace Nexus.Gateways.Composition;

public static class GatewaysAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusGatewaysAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorGatewayCredentialsSearchService, AdministratorGatewayCredentialsSearchService>();
        services.AddScoped<IAdministratorGatewayCredentialsCommandService, AdministratorGatewayCredentialsCommandService>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
