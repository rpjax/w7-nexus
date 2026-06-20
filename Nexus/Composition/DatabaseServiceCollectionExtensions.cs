using Aidan.Mongo.Extensions;
using Nexus.Database.Models;

namespace Nexus.Composition;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddNexusDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var mongo = configuration.GetSection("MongoDB");
        var mongoConnectionString = mongo["ConnectionString"];
        var mongoDatabaseName = mongo["DatabaseName"];

        if (string.IsNullOrWhiteSpace(mongoConnectionString))
            throw new InvalidOperationException(
                "MongoDB:ConnectionString is required. Set it in appsettings.json (or configuration/environment).");

        if (string.IsNullOrWhiteSpace(mongoDatabaseName))
            throw new InvalidOperationException(
                "MongoDB:DatabaseName is required. Set it in appsettings.json (or configuration/environment).");

        services.AddMongoDatabase(mongoConnectionString, mongoDatabaseName);
        services.AddMongoCollection<AccountRecord>("accounts");
        services.AddMongoCollection<FrendzApiCredentialsRecord>("frendz_api_credentials");
        services.AddMongoCollection<SigiloPayApiCredentialsRecord>("sigilopay_api_credentials");
        services.AddMongoCollection<WintechApiCredentialsRecord>("wintech_api_credentials");
        services.AddMongoCollection<PaymentRecord>("payments");
        services.AddMongoCollection<OperationRecord>("operations");
        services.AddMongoCollection<TeamRecord>("teams");
        services.AddMongoCollection<GatewayCredentialsGroupRecord>("gateway_credentials_groups");
        services.AddMongoCollection<BankAccountRecord>("bank_accounts");
        services.AddMongoCollection<CryptoWalletRecord>("crypto_wallets");
        services.AddMongoCollection<WithdrawalRecord>("withdrawals");

        return services;
    }
}
