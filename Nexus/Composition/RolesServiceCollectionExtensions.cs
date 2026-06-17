using Nexus.Administrator.Application.Contracts;
using Nexus.Administrator.Application.Services;
using Nexus.Authorization.Application.Contracts;
using Nexus.OperationAdministrator.Application.Contracts;
using Nexus.OperationAdministrator.Application.Services;
using Nexus.Operator.Application.Contracts;
using Nexus.Operator.Application.Services;
using Nexus.StrawMan.Application.Contracts;
using Nexus.StrawMan.Application.Services;
using Nexus.TeamLeader.Application.Contracts;
using Nexus.TeamLeader.Application.Services;

namespace Nexus.Composition;

public static class RolesServiceCollectionExtensions
{
    public static IServiceCollection AddNexusRoles(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IRequesterIdentityResolver, Nexus.Authorization.Application.Services.RequesterIdentityResolver>();
        services.AddScoped<IOperationAdministratorAccessPolicy, OperationAdministratorAccessPolicy>();
        services.AddScoped<IOperationAdministratorOperationSearchService, OperationAdministratorOperationSearchService>();
        services.AddScoped<IOperationAdministratorAccountSearchService, OperationAdministratorAccountSearchService>();
        services.AddScoped<IOperationAdministratorTeamCommandService, OperationAdministratorTeamCommandService>();
        services.AddScoped<IOperationAdministratorTeamLeaderCandidateSearchService, OperationAdministratorTeamLeaderCandidateSearchService>();
        services.AddScoped<IOperationAdministratorStrawManAssignmentSearchService, OperationAdministratorStrawManAssignmentSearchService>();
        services.AddScoped<IOperationAdministrator, Nexus.OperationAdministrator.Application.Services.OperationAdministrator>();
        services.AddScoped<ITeamLeaderAccessPolicy, TeamLeaderAccessPolicy>();
        services.AddScoped<IOperatorAccessPolicy, OperatorAccessPolicy>();
        services.AddScoped<IOperatorOperationSearchService, OperatorOperationSearchService>();
        services.AddScoped<IStrawManAccessPolicy, StrawManAccessPolicy>();

        services.AddScoped<IAdministratorOperationSearchService, AdministratorOperationSearchService>();
        services.AddScoped<IAdministratorAccountSearchService, AdministratorAccountSearchService>();
        services.AddScoped<IAdministratorOperationCommandService, AdministratorOperationCommandService>();
        services.AddScoped<IAdministratorTeamCommandService, AdministratorTeamCommandService>();
        services.AddScoped<IAdministratorTeamOperatorCommandService, AdministratorTeamOperatorCommandService>();
        services.AddScoped<IAdministratorOperatorAssignmentSearchService, AdministratorOperatorAssignmentSearchService>();
        services.AddScoped<IAdministratorProfitShareAccountSearchService, AdministratorProfitShareAccountSearchService>();
        services.AddScoped<ITeamLeaderLedTeamsSearchService, TeamLeaderLedTeamsSearchService>();
        services.AddScoped<ITeamLeaderTeamCommandService, TeamLeaderTeamCommandService>();
        services.AddScoped<ITeamLeaderOperatorAssignmentSearchService, TeamLeaderOperatorAssignmentSearchService>();
        services.AddScoped<ITeamLeaderProfitShareAccountSearchService, TeamLeaderProfitShareAccountSearchService>();
        services.AddScoped<IAdministrator, Nexus.Administrator.Application.Services.Administrator>();
        services.AddScoped<Nexus.Administrator.Application.Contracts.ITeamGatewayDetailsLoader, Nexus.Administrator.Application.Services.TeamGatewayDetailsLoader>();
        services.AddScoped<Nexus.OperationAdministrator.Application.Contracts.ITeamGatewayDetailsLoader, Nexus.OperationAdministrator.Application.Services.TeamGatewayDetailsLoader>();
        services.AddScoped<ITeamLeader, Nexus.TeamLeader.Application.Services.TeamLeader>();
        services.AddScoped<IOperator, Nexus.Operator.Application.Services.Operator>();
        services.AddScoped<IStrawMan, Nexus.StrawMan.Application.Services.StrawMan>();

        return services;
    }
}
