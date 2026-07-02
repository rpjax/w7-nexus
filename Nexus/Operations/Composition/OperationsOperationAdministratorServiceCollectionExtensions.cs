using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Services;

namespace Nexus.Operations.Composition;

public static class OperationsOperationAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperationsOperationAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IOperationAdministratorAccessPolicy, OperationAdministratorAccessPolicy>();
        services.AddScoped<IOperationAdministratorOperationSearchService, OperationAdministratorOperationSearchService>();
        services.AddScoped<IOperationAdministratorAccountSearchService, OperationAdministratorAccountSearchService>();
        services.AddScoped<IOperationAdministratorTeamCommandService, OperationAdministratorTeamCommandService>();
        services.AddScoped<IOperationAdministratorOperationCommandService, OperationAdministratorOperationCommandService>();
        services.AddScoped<IOperationAdministratorTeamLeaderCandidateSearchService, OperationAdministratorTeamLeaderCandidateSearchService>();
        services.AddScoped<IOperationAdministratorStrawManAssignmentSearchService, OperationAdministratorStrawManAssignmentSearchService>();
        services.AddScoped<IOperationAdministrator, OperationAdministrator>();

        return services;
    }
}
