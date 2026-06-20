using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.OperationAdministrators.Application.Services;

namespace Nexus.OperationAdministrators.Composition;

public static class OperationAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperationAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IOperationAdministratorAccessPolicy, OperationAdministratorAccessPolicy>();
        services.AddScoped<IOperationAdministratorOperationSearchService, OperationAdministratorOperationSearchService>();
        services.AddScoped<IOperationAdministratorAccountSearchService, OperationAdministratorAccountSearchService>();
        services.AddScoped<IOperationAdministratorTeamCommandService, OperationAdministratorTeamCommandService>();
        services.AddScoped<IOperationAdministratorOperationCommandService, OperationAdministratorOperationCommandService>();
        services.AddScoped<IOperationAdministratorTeamLeaderCandidateSearchService, OperationAdministratorTeamLeaderCandidateSearchService>();
        services.AddScoped<IOperationAdministratorStrawManAssignmentSearchService, OperationAdministratorStrawManAssignmentSearchService>();
        services.AddScoped<IOperationAdministratorWithdrawalCommandService, OperationAdministratorWithdrawalCommandService>();
        services.AddScoped<IOperationAdministrator, OperationAdministrator>();
        services.AddScoped<ITeamGatewayDetailsLoader, TeamGatewayDetailsLoader>();

        return services;
    }
}
