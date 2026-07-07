using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Services;

namespace Nexus.Payments.Composition;

public static class PaymentsAdministratorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusPaymentsAdministrator(this IServiceCollection services)
    {
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();
        services.AddScoped<IAdministratorPaymentSearchService, AdministratorPaymentSearchService>();
        services.AddScoped<IAdministratorPaymentCommandService, AdministratorPaymentCommandService>();
        services.AddScoped<IAdministrator, Administrator>();

        return services;
    }
}
