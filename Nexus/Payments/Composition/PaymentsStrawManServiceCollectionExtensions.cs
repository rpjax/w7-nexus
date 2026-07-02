using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Services;

namespace Nexus.Payments.Composition;

public static class PaymentsStrawManServiceCollectionExtensions
{
    public static IServiceCollection AddNexusPaymentsStrawMan(this IServiceCollection services)
    {
        services.AddScoped<IStrawManAccessPolicy, StrawManAccessPolicy>();
        services.AddScoped<IStrawManPaymentSearchService, StrawManPaymentSearchService>();
        services.AddScoped<IStrawMan, StrawMan>();

        return services;
    }
}
