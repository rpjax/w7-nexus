using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Application.Services;
using Nexus.AccountNodes.Infrastructure.Persistance;

namespace Nexus.AccountNodes.Composition;

public static class AccountNodesServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAccountNodes(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<ICryptoWalletService, CryptoWalletService>();
        services.AddScoped<IBalanceSplitCalculationService, BalanceSplitCalculationService>();
        services.AddScoped<IBankAccountRepository, MongoBankAccountRepository>();
        services.AddScoped<ICryptoWalletRepository, MongoCryptoWalletRepository>();

        return services;
    }
}
