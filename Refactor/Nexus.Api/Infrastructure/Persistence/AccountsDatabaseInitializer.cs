using Npgsql;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;

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

            do $$
            begin
                if to_regclass('public.retired_handles') is not null
                   and to_regclass('public.retired_usernames') is null then
                    alter table retired_handles rename to retired_usernames;
                    alter table retired_usernames rename column handle_lower to username_lower;
                    alter table retired_usernames rename column original_handle to original_username;
                end if;
            end $$;

            create table if not exists account_secrets (
                account_id uuid primary key,
                password_hash text not null
            );

            create table if not exists account_usernames (
                username_lower varchar(64) primary key,
                account_id uuid not null unique
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
        await scope.ServiceProvider.GetRequiredService<MartenAccountRepository>().BackfillLegacyAsync();
    }
}
