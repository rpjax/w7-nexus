using Npgsql;

namespace Refactor.Nexus.Api.Infrastructure.Persistence;

public interface IAccountsDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class AccountsDatabaseInitializer : IAccountsDatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public AccountsDatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists accounts (
                id uuid primary key,
                username varchar(64) not null,
                password_hash text not null,
                status varchar(16) not null default 'Active',
                roles text[] not null default '{}'::text[],
                permissions text[] not null default '{}'::text[],
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );

            alter table accounts
                add column if not exists status varchar(16) not null default 'Active';

            create unique index if not exists ix_accounts_username_lower
                on accounts (lower(username));

            create table if not exists retired_handles (
                handle_lower varchar(64) primary key,
                original_handle varchar(64) not null,
                retired_from uuid not null,
                retired_at timestamptz not null
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class AccountsDatabaseInitializerExtensions
{
    public static async Task InitializeAccountsDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IAccountsDatabaseInitializer>();
        await initializer.InitializeAsync();
    }
}
