using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Services;

namespace Nexus.Operations.Composition;

public static class OperationsAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperationsAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorOperationSearchService, AdministratorOperationSearchService>();
        services.AddScoped<IAdministratorOperationCommandService, AdministratorOperationCommandService>();
        services.AddScoped<IAdministratorTeamCommandService, AdministratorTeamCommandService>();
        services.AddScoped<IAdministratorTeamOperatorCommandService, AdministratorTeamOperatorCommandService>();
        services.AddScoped<IAdministratorOperatorAssignmentSearchService, AdministratorOperatorAssignmentSearchService>();
        services.AddScoped<IAdministratorProfitShareAccountSearchService, AdministratorProfitShareAccountSearchService>();
        services.AddScoped<IAdministratorOperationPickerSearchService, AdministratorOperationPickerSearchService>();
        services.AddScoped<ITeamGatewayDetailsLoader, TeamGatewayDetailsLoader>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
