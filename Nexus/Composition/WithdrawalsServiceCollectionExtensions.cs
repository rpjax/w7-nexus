using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Application.Services;
using Nexus.Withdrawals.Infrastructure.Persistance;

namespace Nexus.Composition;

public static class WithdrawalsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusWithdrawals(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<ICryptoWalletService, CryptoWalletService>();
        services.AddScoped<IWithdrawalService, WithdrawalService>();
        services.AddScoped<IBankAccountRepository, MongoBankAccountRepository>();
        services.AddScoped<ICryptoWalletRepository, MongoCryptoWalletRepository>();
        services.AddScoped<IWithdrawalRepository, MongoWithdrawalRepository>();

        return services;
    }
}
