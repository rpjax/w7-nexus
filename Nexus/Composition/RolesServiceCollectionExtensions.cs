using Nexus.Authorization.Composition;
using Nexus.Olx.Composition;

namespace Nexus.Composition;

public static class RolesServiceCollectionExtensions
{
    public static IServiceCollection AddNexusRoles(this IServiceCollection services)
    {
        // Shared authorization
        services.AddNexusAuthorization();

        // OLX
        services.AddNexusOlx();

        return services;
    }
}
