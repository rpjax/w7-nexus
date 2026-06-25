using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Application.Services;
using Nexus.BankAccounts.Infrastructure.Persistance;

namespace Nexus.BankAccounts.Composition;

public static class BankAccountsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusBankAccounts(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<IBankAccountRepository, MongoBankAccountRepository>();

        return services;
    }
}
