using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Services;

namespace Nexus.Administrators.Composition;

public static class AdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorOperationSearchService, AdministratorOperationSearchService>();
        services.AddScoped<IAdministratorAccountSearchService, AdministratorAccountSearchService>();
        services.AddScoped<IAdministratorAccountCommandService, AdministratorAccountCommandService>();
        services.AddScoped<IAdministratorOperationCommandService, AdministratorOperationCommandService>();
        services.AddScoped<IAdministratorTeamCommandService, AdministratorTeamCommandService>();
        services.AddScoped<IAdministratorTeamOperatorCommandService, AdministratorTeamOperatorCommandService>();
        services.AddScoped<IAdministratorOperatorAssignmentSearchService, AdministratorOperatorAssignmentSearchService>();
        services.AddScoped<IAdministratorProfitShareAccountSearchService, AdministratorProfitShareAccountSearchService>();
        services.AddScoped<IAdministratorOperationPickerSearchService, AdministratorOperationPickerSearchService>();
        services.AddScoped<IAdministratorWithdrawalCommandService, AdministratorWithdrawalCommandService>();
        services.AddScoped<IAdministrator, Administrator>();
        services.AddScoped<ITeamGatewayDetailsLoader, TeamGatewayDetailsLoader>();

        return services;
    }
}
