using Microsoft.Extensions.DependencyInjection;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Services;

namespace Nexus.Charges.Composition;

public static class ChargesServiceCollectionExtensions
{
    public static IServiceCollection AddNexusCharges(this IServiceCollection services)
    {
        services.AddScoped<IGatewayCredentialsResolver, GatewayCredentialsResolver>();
        services.AddScoped<IChargeProfitShareResolver, ChargeProfitShareResolver>();
        services.AddScoped<IChargeSplitCalculationService, ChargeSplitCalculationService>();
        services.AddScoped<IChargeService, ChargeService>();
        return services;
    }
}
