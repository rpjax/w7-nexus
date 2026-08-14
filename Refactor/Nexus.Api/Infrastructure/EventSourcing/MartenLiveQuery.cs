using Marten;
using Npgsql;

namespace Refactor.Nexus.Api.Infrastructure.EventSourcing;

public static class MartenLiveQuery
{
    public static async Task<IReadOnlyList<T>> ListAsync<T>(
        IDocumentStore store,
        string streamPrefix,
        CancellationToken cancellationToken)
        where T : class, new()
    {
        var keys = await ListStreamKeysAsync(store, streamPrefix, cancellationToken);
        if (keys.Count == 0)
            return [];

        await using var session = store.LightweightSession();
        var items = new List<T>(keys.Count);
        foreach (var key in keys)
        {
            var aggregate = await LoadAsync<T>(session, key, cancellationToken);
            if (aggregate is not null)
                items.Add(aggregate);
        }

        return items;
    }

    public static async Task<T?> LoadAsync<T>(
        IDocumentStore store,
        string streamKey,
        CancellationToken cancellationToken)
        where T : class, new()
    {
        await using var session = store.LightweightSession();
        return await LoadAsync<T>(session, streamKey, cancellationToken);
    }

    public static async Task<T?> LoadAsync<T>(
        IQuerySession session,
        string streamKey,
        CancellationToken cancellationToken)
        where T : class, new()
    {
        var events = await session.Events.FetchStreamAsync(streamKey, token: cancellationToken);
        if (events.Count == 0)
            return null;

        return Fold<T>(events.Select(e => e.Data));
    }

    public static T Fold<T>(IEnumerable<object> events)
        where T : class, new()
    {
        dynamic aggregate = new T();
        foreach (var @event in events)
            aggregate.Apply((dynamic)@event);

        return aggregate;
    }

    private static async Task<IReadOnlyList<string>> ListStreamKeysAsync(
        IDocumentStore store,
        string streamPrefix,
        CancellationToken cancellationToken)
    {
        await using var connection = store.Storage.Database.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "select id from nexus_es.mt_streams where id like @prefix";
            var prefix = command.CreateParameter();
            prefix.ParameterName = "prefix";
            prefix.Value = streamPrefix + "%";
            command.Parameters.Add(prefix);

            var keys = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                keys.Add(reader.GetString(0));

            return keys.Distinct(StringComparer.Ordinal).ToList();
        }
        catch (PostgresException ex) when (
            ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName)
        {
            return [];
        }
    }
}
