using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Charging.Infrastructure.Persistence;

public interface IChargingDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class ChargingDatabaseInitializer : IChargingDatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public ChargingDatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists operation_emission_accounts (
                operation_id uuid not null,
                world_account_id uuid not null,
                primary key (operation_id, world_account_id)
            );

            create index if not exists ix_operation_emission_accounts_account
                on operation_emission_accounts (world_account_id);

            do $$
            begin
                if to_regclass('public.operation_emission_rails') is not null then
                    insert into operation_emission_accounts (operation_id, world_account_id)
                    select operation_id, rail_id
                    from operation_emission_rails
                    on conflict do nothing;
                end if;
            end $$;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class ChargingDatabaseInitializerExtensions
{
    public static async Task InitializeChargingDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IChargingDatabaseInitializer>();
        await initializer.InitializeAsync();
    }
}

public sealed class PostgresOperationEmissionSetRepository : IOperationEmissionSetRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresOperationEmissionSetRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Guid>> ListRailIdsAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select world_account_id from operation_emission_accounts
            where operation_id = @op
            order by world_account_id
            """;
        command.Parameters.AddWithValue("op", operationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetGuid(0));
        return ids;
    }

    public async Task BindAsync(Guid operationId, Guid railId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into operation_emission_accounts (operation_id, world_account_id)
            values (@op, @account)
            on conflict do nothing
            """;
        command.Parameters.AddWithValue("op", operationId);
        command.Parameters.AddWithValue("account", railId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UnbindAsync(Guid operationId, Guid railId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            delete from operation_emission_accounts
            where operation_id = @op and world_account_id = @account
            """;
        command.Parameters.AddWithValue("op", operationId);
        command.Parameters.AddWithValue("account", railId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
