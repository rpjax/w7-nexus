using Marten;
using Marten.Events.Projections;
using Npgsql;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using Refactor.Nexus.Api.WorldAccounts.Domain.Events;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.WorldAccounts.Infrastructure.Persistence;

public sealed class MartenWorldAccountRepository : IWorldAccountRepository
{
    private readonly IDocumentStore _store;
    private readonly INpgsqlConnectionFactory _connections;

    public MartenWorldAccountRepository(IDocumentStore store, INpgsqlConnectionFactory connections)
    {
        _store = store;
        _connections = connections;
    }

    public async Task<WorldAccountAggregate?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        var account = await session.Events.AggregateStreamAsync<WorldAccountAggregate>(
            EventStoreStreams.WorldAccount(accountId), token: cancellationToken);
        if (account is null || account.Id == Guid.Empty)
            return null;
        return account;
    }

    public async Task SaveAsync(WorldAccountAggregate account, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        await MartenStreamWriter.SaveAsync(
            session,
            EventStoreStreams.WorldAccount(account.Id),
            typeof(WorldAccountAggregate),
            account.UncommittedEvents,
            cancellationToken);
        account.ClearUncommitted();
    }

    public async Task<IReadOnlyList<WorldAccountAggregate>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        var items = await session.Query<WorldAccountAggregate>().ToListAsync(cancellationToken);
        return items.Where(a => a.Id != Guid.Empty).OrderBy(a => a.Label).ToList();
    }

    public async Task<IReadOnlyList<WorldAccountTransaction>> ListTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        var events = await session.Events.FetchStreamAsync(EventStoreStreams.WorldAccount(accountId), token: cancellationToken);
        var items = new List<WorldAccountTransaction>();
        foreach (var wrapper in events)
        {
            switch (wrapper.Data)
            {
                case ObservedCredited e:
                    items.Add(new WorldAccountTransaction("Credit", e.Currency, e.Amount, e.Memo, null, e.OccurredAt));
                    break;
                case ObservedDebited e:
                    items.Add(new WorldAccountTransaction("Debit", e.Currency, e.Amount, e.Memo, null, e.OccurredAt));
                    break;
                case QuotaConsumed e:
                    items.Add(new WorldAccountTransaction("QuotaConsumed", e.Currency, e.Amount, null, e.ChargeId, e.OccurredAt));
                    break;
            }
        }

        return items;
    }

    public async Task BackfillLegacyRailsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select id, orange_member_id, level1_cut_percent, currency, quota_remaining, emission_status, created_at, last_updated_at
                from emission_rails
                """;
            var rows = new List<(Guid Id, Guid Orange, decimal Cut, string Currency, decimal Quota, string Status, DateTime Created, DateTime Updated)>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add((
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetDecimal(2),
                        reader.GetString(3),
                        reader.GetDecimal(4),
                        reader.GetString(5),
                        DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
                        DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
                }
            }

            foreach (var row in rows)
            {
                await using var session = _store.LightweightSession();
                var key = EventStoreStreams.WorldAccount(row.Id);
                if (await session.Events.FetchStreamStateAsync(key, cancellationToken) is not null)
                    continue;

                session.Events.StartStream<WorldAccountAggregate>(
                    key,
                    new WorldAccountBackfilled(
                        row.Id,
                        WorldAccountKind.Gateway.ToString(),
                        "Legacy gateway",
                        row.Orange,
                        row.Cut,
                        row.Status,
                        BalanceStatus.Accessible.ToString(),
                        [new CurrencyAmount(row.Currency.ToUpperInvariant(), row.Quota)],
                        [],
                        row.Created,
                        row.Updated));
                await session.SaveChangesAsync(cancellationToken);
            }

            await using var drop = connection.CreateCommand();
            drop.CommandText = """
                drop table if exists operation_emission_rails;
                drop table if exists emission_rails;
                """;
            await drop.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException)
        {
        }
    }

    public static void Configure(StoreOptions options)
    {
        options.Projections.Snapshot<WorldAccountAggregate>(SnapshotLifecycle.Inline);
        options.Schema.For<WorldAccountAggregate>().Identity(x => x.Id);
        options.Events.AddEventTypes(
        [
            typeof(WorldAccountOpened),
            typeof(WorldAccountBackfilled),
            typeof(WorldAccountLabeled),
            typeof(GatewayCutConfigured),
            typeof(GatewayOrangeChanged),
            typeof(EmissionStatusChanged),
            typeof(BalanceStatusChanged),
            typeof(QuotaConfigured),
            typeof(QuotaConsumed),
            typeof(ObservedCredited),
            typeof(ObservedDebited)
        ]);
    }
}
