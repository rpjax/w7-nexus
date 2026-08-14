using Marten;
using Marten.Events.Projections;
using Npgsql;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Events;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;

namespace Refactor.Nexus.Api.Operations.Infrastructure.Persistence;

public sealed class MartenOperationRepository : IOperationRepository, IOperationReadRepository
{
    private readonly IDocumentStore _store;
    private readonly INpgsqlConnectionFactory _connections;

    public MartenOperationRepository(IDocumentStore store, INpgsqlConnectionFactory connections)
    {
        _store = store;
        _connections = connections;
    }

    public async Task<OperationAggregate?> GetByIdAsync(OperationId id, CancellationToken cancellationToken = default)
    {
        var operation = await MartenLiveQuery.LoadAsync<OperationAggregate>(
            _store, EventStoreStreams.Operation(id.Value), cancellationToken);
        if (operation is null || operation.Id.Value == Guid.Empty)
            return null;
        return operation;
    }

    public async Task SaveAsync(OperationAggregate operation, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        await MartenStreamWriter.SaveAsync(
            session,
            EventStoreStreams.Operation(operation.Id.Value),
            typeof(OperationAggregate),
            operation.UncommittedEvents,
            cancellationToken);
        operation.ClearUncommitted();
    }

    public async Task<OperationAggregate?> GetByKeyAsync(OperationKey key, CancellationToken cancellationToken = default)
    {
        var all = await ListAsync(cancellationToken);
        return all.FirstOrDefault(o => o.Key.Value == key.Value);
    }

    public async Task<bool> ExistsAsync(OperationId id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken) is not null;

    public async Task<bool> IsMemberAssignedAsync(
        OperationId operationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var operation = await GetByIdAsync(operationId, cancellationToken);
        return operation is not null && operation.IsAssigned(memberId);
    }

    public async Task<bool> IsMemberAssignedToAnyAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var all = await ListAsync(cancellationToken);
        return all.Any(o => o.IsAssigned(memberId));
    }

    public async Task<IReadOnlyList<OperationAggregate>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await MartenLiveQuery.ListAsync<OperationAggregate>(_store, "operation-", cancellationToken);
        return items.Where(o => o.Id.Value != Guid.Empty).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task BackfillLegacyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select id, operation_key, name, status, management_cut_percent, created_at, last_updated_at
                from operations
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                await using var session = _store.LightweightSession();
                var key = EventStoreStreams.Operation(id);
                if (await session.Events.FetchStreamStateAsync(key, cancellationToken) is not null)
                    continue;

                var assigned = await LoadAssignmentsAsync(connection, id, cancellationToken);
                decimal? cut = reader.IsDBNull(4) ? null : reader.GetDecimal(4);
                session.Events.StartStream<OperationAggregate>(
                    key,
                    new OperationBackfilled(
                        id,
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        cut,
                        assigned,
                        DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
                        DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)));
                await session.SaveChangesAsync(cancellationToken);
            }
        }
        catch (PostgresException)
        {
        }
    }

    public static void Configure(StoreOptions options)
    {
        options.Events.AddEventTypes(
        [
            typeof(OperationOpened),
            typeof(OperationBackfilled),
            typeof(OperationTransitioned),
            typeof(OperationAssignmentsCleared),
            typeof(OperationManagementCutConfigured),
            typeof(OperatorAssigned),
            typeof(OperatorUnassigned)
        ]);
    }

    private static async Task<Guid[]> LoadAssignmentsAsync(
        NpgsqlConnection connection,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select member_id from operation_assignments where operation_id = @id";
        command.Parameters.AddWithValue("id", operationId);
        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetGuid(0));
        return ids.ToArray();
    }
}
