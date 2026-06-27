using Nexus.Administrators.Composition;
using Nexus.Authorization.Composition;
using Nexus.OperationAdministrators.Composition;
using Nexus.Operators.Composition;
using Nexus.Olx.Composition;
using Nexus.StrawMen.Composition;
using Nexus.TeamLeaders.Composition;

namespace Nexus.Composition;

public static class RolesServiceCollectionExtensions
{
    public static IServiceCollection AddNexusRoles(this IServiceCollection services)
    {
        // Shared authorization
        services.AddNexusAuthorization();

        // Operation administrator
        services.AddNexusOperationAdministrator();

        // Administrator
        services.AddNexusAdministrator();

        // Team leader
        services.AddNexusTeamLeader();

        // Operator
        services.AddNexusOperator();

        // Straw man
        services.AddNexusStrawMan();

        // OLX
        services.AddNexusOlx();

        return services;
    }
}
