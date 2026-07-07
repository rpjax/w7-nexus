using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Services;

namespace Nexus.Charges.Composition;

public static class ChargesAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusChargesAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
