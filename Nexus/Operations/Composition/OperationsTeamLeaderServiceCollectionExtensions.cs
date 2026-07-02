using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Services;

namespace Nexus.Operations.Composition;

public static class OperationsTeamLeaderServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperationsTeamLeader(this IServiceCollection services)
    {
        services.AddScoped<ITeamLeaderAccessPolicy, TeamLeaderAccessPolicy>();
        services.AddScoped<ITeamLeaderLedTeamsSearchService, TeamLeaderLedTeamsSearchService>();
        services.AddScoped<ITeamLeaderTeamCommandService, TeamLeaderTeamCommandService>();
        services.AddScoped<ITeamLeaderOperatorAssignmentSearchService, TeamLeaderOperatorAssignmentSearchService>();
        services.AddScoped<ITeamLeaderProfitShareAccountSearchService, TeamLeaderProfitShareAccountSearchService>();
        services.AddScoped<ITeamLeader, TeamLeader>();

        return services;
    }
}
