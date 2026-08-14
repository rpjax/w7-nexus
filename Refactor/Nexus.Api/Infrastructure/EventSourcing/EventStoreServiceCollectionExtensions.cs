using JasperFx.Events;
using Marten;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;
using Refactor.Nexus.Api.Charging.Infrastructure.Persistence;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Mandates.Infrastructure.Persistence;
using Refactor.Nexus.Api.Operations.Infrastructure.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Infrastructure.Persistence;
using Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Infrastructure.EventSourcing;

public static class EventStoreServiceCollectionExtensions
{
    public static IServiceCollection AddNexusEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(d => d.ServiceType == typeof(IDocumentStore)))
            return services;

        var connectionString = NexusDbConnection.Resolve(configuration);
        services.AddMarten(options =>
            {
                options.Connection(connectionString);
                options.DatabaseSchemaName = "nexus_es";
                options.Events.StreamIdentity = StreamIdentity.AsString;
                MartenAccountRepository.Configure(options);
                MartenMandateRepositories.Configure(options);
                MartenOperationRepository.Configure(options);
                MartenChargeRepository.Configure(options);
                MartenWorldAccountRepository.Configure(options);
                MartenClaimRepository.Configure(options);
            })
            .UseLightweightSessions()
            .ApplyAllDatabaseChangesOnStartup();

        return services;
    }
}
