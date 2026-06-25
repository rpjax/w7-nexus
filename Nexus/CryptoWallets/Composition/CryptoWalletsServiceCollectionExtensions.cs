using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Application.Services;
using Nexus.CryptoWallets.Infrastructure.Persistance;

namespace Nexus.CryptoWallets.Composition;

public static class CryptoWalletsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusCryptoWallets(this IServiceCollection services)
    {
        services.AddScoped<ICryptoWalletService, CryptoWalletService>();
        services.AddScoped<ICryptoWalletRepository, MongoCryptoWalletRepository>();

        return services;
    }
}
