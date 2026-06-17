using Nexus.TeamLeaders.Application.Contracts;
using Nexus.TeamLeaders.Application.Services;

namespace Nexus.TeamLeaders.Composition;

public static class TeamLeaderServiceCollectionExtensions
{
    public static IServiceCollection AddNexusTeamLeader(this IServiceCollection services)
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
