using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application.Services;

namespace Nexus.Accounts.Composition;

public static class AccountsAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAccountsAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorAccountSearchService, AdministratorAccountSearchService>();
        services.AddScoped<IAdministratorAccountCommandService, AdministratorAccountCommandService>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
