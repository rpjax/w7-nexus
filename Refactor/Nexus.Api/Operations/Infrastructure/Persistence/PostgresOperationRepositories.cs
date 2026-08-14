using Npgsql;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;
using ScriptArtifact = Refactor.Nexus.Api.Operations.Domain.Aggregates.Script.ScriptArtifact;
using StoreObject = Refactor.Nexus.Api.Operations.Domain.Aggregates.Store.StoreObject;

namespace Refactor.Nexus.Api.Operations.Infrastructure.Persistence;

public interface IOperationsDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class OperationsDatabaseInitializer : IOperationsDatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public OperationsDatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists operations (
                id uuid primary key,
                operation_key varchar(64) not null unique,
                name varchar(128) not null,
                status varchar(16) not null,
                management_cut_percent numeric(7,4) null,
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );

            create table if not exists operation_assignments (
                operation_id uuid not null references operations(id) on delete cascade,
                member_id uuid not null,
                primary key (operation_id, member_id)
            );

            create index if not exists ix_operation_assignments_member
                on operation_assignments (member_id);

            create table if not exists script_artifacts (
                id uuid primary key,
                operation_key varchar(64) not null,
                name varchar(128) not null,
                body text not null,
                enabled boolean not null default true,
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );

            create index if not exists ix_script_artifacts_key
                on script_artifacts (operation_key);

            create table if not exists store_objects (
                id uuid primary key,
                operation_key varchar(64) not null,
                object_type varchar(128) not null,
                payload_json text not null,
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );

            create index if not exists ix_store_objects_key
                on store_objects (operation_key);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class OperationsDatabaseInitializerExtensions
{
    public static async Task InitializeOperationsDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IOperationsDatabaseInitializer>();
        await initializer.InitializeAsync();
        await scope.ServiceProvider.GetRequiredService<MartenOperationRepository>().BackfillLegacyAsync();
    }
}

public sealed class PostgresOperationRepository : IOperationRepository, IOperationReadRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresOperationRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OperationAggregate?> GetByIdAsync(OperationId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, status, management_cut_percent, created_at, last_updated_at
            from operations where id = @id
            """;
        command.Parameters.AddWithValue("id", id.Value);
        var op = await ReadOperationAsync(command, cancellationToken);
        if (op is null) return null;
        var assignments = await LoadAssignmentsAsync(connection, id.Value, cancellationToken);
        return OperationAggregate.Rehydrate(
            op.Id, op.Key, op.Name, op.Status, op.ManagementCutPercent, assignments, op.CreatedAt, op.LastUpdatedAt);
    }

    public async Task<OperationAggregate?> GetByKeyAsync(OperationKey key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, status, management_cut_percent, created_at, last_updated_at
            from operations where operation_key = @key
            """;
        command.Parameters.AddWithValue("key", key.Value);
        var op = await ReadOperationAsync(command, cancellationToken);
        if (op is null) return null;
        var assignments = await LoadAssignmentsAsync(connection, op.Id.Value, cancellationToken);
        return OperationAggregate.Rehydrate(
            op.Id, op.Key, op.Name, op.Status, op.ManagementCutPercent, assignments, op.CreatedAt, op.LastUpdatedAt);
    }

    public async Task<bool> ExistsAsync(OperationId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from operations where id = @id";
        command.Parameters.AddWithValue("id", id.Value);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    public async Task<bool> IsMemberAssignedAsync(OperationId operationId, Guid memberId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select 1 from operation_assignments
            where operation_id = @operation_id and member_id = @member_id
            """;
        command.Parameters.AddWithValue("operation_id", operationId.Value);
        command.Parameters.AddWithValue("member_id", memberId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    public async Task<bool> IsMemberAssignedToAnyAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from operation_assignments where member_id = @member_id limit 1";
        command.Parameters.AddWithValue("member_id", memberId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    public async Task<IReadOnlyList<OperationAggregate>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, status, management_cut_percent, created_at, last_updated_at
            from operations
            order by created_at desc
            """;
        var shells = new List<OperationAggregate>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                shells.Add(MapShell(reader));
        }

        var result = new List<OperationAggregate>();
        foreach (var shell in shells)
        {
            var assignments = await LoadAssignmentsAsync(connection, shell.Id.Value, cancellationToken);
            result.Add(OperationAggregate.Rehydrate(
                shell.Id, shell.Key, shell.Name, shell.Status, shell.ManagementCutPercent,
                assignments, shell.CreatedAt, shell.LastUpdatedAt));
        }

        return result;
    }

    public async Task SaveAsync(OperationAggregate operation, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = tx;
            upsert.CommandText = """
                insert into operations
                    (id, operation_key, name, status, management_cut_percent, created_at, last_updated_at)
                values
                    (@id, @key, @name, @status, @cut, @created_at, @last_updated_at)
                on conflict (id) do update set
                    name = excluded.name,
                    status = excluded.status,
                    management_cut_percent = excluded.management_cut_percent,
                    last_updated_at = excluded.last_updated_at
                """;
            upsert.Parameters.AddWithValue("id", operation.Id.Value);
            upsert.Parameters.AddWithValue("key", operation.Key.Value);
            upsert.Parameters.AddWithValue("name", operation.Name);
            upsert.Parameters.AddWithValue("status", operation.Status.ToString());
            upsert.Parameters.AddWithValue("cut", (object?)operation.ManagementCutPercent ?? DBNull.Value);
            upsert.Parameters.AddWithValue("created_at", operation.CreatedAt);
            upsert.Parameters.AddWithValue("last_updated_at", operation.LastUpdatedAt);
            await upsert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = "delete from operation_assignments where operation_id = @id";
            delete.Parameters.AddWithValue("id", operation.Id.Value);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var memberId in operation.AssignedOperatorIds)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                insert into operation_assignments (operation_id, member_id)
                values (@operation_id, @member_id)
                """;
            insert.Parameters.AddWithValue("operation_id", operation.Id.Value);
            insert.Parameters.AddWithValue("member_id", memberId);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Guid>> LoadAssignmentsAsync(
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
        return ids;
    }

    private static async Task<OperationAggregate?> ReadOperationAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return MapShell(reader);
    }

    private static OperationAggregate MapShell(NpgsqlDataReader reader) =>
        OperationAggregate.Rehydrate(
            new OperationId(reader.GetGuid(0)),
            new OperationKey(reader.GetString(1)),
            reader.GetString(2),
            Enum.Parse<OperationStatus>(reader.GetString(3), ignoreCase: true),
            reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            [],
            reader.GetDateTime(5),
            reader.GetDateTime(6));
}

public sealed class PostgresScriptArtifactRepository : IScriptArtifactRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresScriptArtifactRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ScriptArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, body, enabled, created_at, last_updated_at
            from script_artifacts where id = @id
            """;
        command.Parameters.AddWithValue("id", id);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<ScriptArtifact?> GetEnabledByKeyAsync(OperationKey key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, body, enabled, created_at, last_updated_at
            from script_artifacts
            where operation_key = @key and enabled = true
            order by last_updated_at desc
            limit 1
            """;
        command.Parameters.AddWithValue("key", key.Value);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task SaveAsync(ScriptArtifact script, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into script_artifacts
                (id, operation_key, name, body, enabled, created_at, last_updated_at)
            values
                (@id, @key, @name, @body, @enabled, @created_at, @last_updated_at)
            on conflict (id) do update set
                name = excluded.name,
                body = excluded.body,
                enabled = excluded.enabled,
                last_updated_at = excluded.last_updated_at
            """;
        command.Parameters.AddWithValue("id", script.Id);
        command.Parameters.AddWithValue("key", script.OperationKey.Value);
        command.Parameters.AddWithValue("name", script.Name);
        command.Parameters.AddWithValue("body", script.Body);
        command.Parameters.AddWithValue("enabled", script.Enabled);
        command.Parameters.AddWithValue("created_at", script.CreatedAt);
        command.Parameters.AddWithValue("last_updated_at", script.LastUpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScriptArtifact>> ListByKeyAsync(OperationKey key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, name, body, enabled, created_at, last_updated_at
            from script_artifacts where operation_key = @key
            order by name
            """;
        command.Parameters.AddWithValue("key", key.Value);
        var items = new List<ScriptArtifact>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    private static async Task<ScriptArtifact?> ReadOneAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return Map(reader);
    }

    private static ScriptArtifact Map(NpgsqlDataReader reader) =>
        ScriptArtifact.Rehydrate(
            reader.GetGuid(0),
            new OperationKey(reader.GetString(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetBoolean(4),
            reader.GetDateTime(5),
            reader.GetDateTime(6));
}

public sealed class PostgresStoreObjectRepository : IStoreObjectRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresStoreObjectRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<StoreObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, object_type, payload_json, created_at, last_updated_at
            from store_objects where id = @id
            """;
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return Map(reader);
    }

    public async Task SaveAsync(StoreObject storeObject, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into store_objects
                (id, operation_key, object_type, payload_json, created_at, last_updated_at)
            values
                (@id, @key, @type, @payload, @created_at, @last_updated_at)
            on conflict (id) do update set
                object_type = excluded.object_type,
                payload_json = excluded.payload_json,
                last_updated_at = excluded.last_updated_at
            """;
        command.Parameters.AddWithValue("id", storeObject.Id);
        command.Parameters.AddWithValue("key", storeObject.OperationKey.Value);
        command.Parameters.AddWithValue("type", storeObject.ObjectType);
        command.Parameters.AddWithValue("payload", storeObject.PayloadJson);
        command.Parameters.AddWithValue("created_at", storeObject.CreatedAt);
        command.Parameters.AddWithValue("last_updated_at", storeObject.LastUpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "delete from store_objects where id = @id";
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoreObject>> ListByKeyAsync(
        OperationKey key,
        string? objectType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, operation_key, object_type, payload_json, created_at, last_updated_at
            from store_objects
            where operation_key = @key
              and (@type is null or object_type = @type)
            order by last_updated_at desc
            """;
        command.Parameters.AddWithValue("key", key.Value);
        command.Parameters.AddWithValue("type", (object?)objectType ?? DBNull.Value);
        var items = new List<StoreObject>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    private static StoreObject Map(NpgsqlDataReader reader) =>
        StoreObject.Rehydrate(
            reader.GetGuid(0),
            new OperationKey(reader.GetString(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDateTime(4),
            reader.GetDateTime(5));
}
