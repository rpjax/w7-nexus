using Npgsql;
using Refactor.Nexus.Api.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Journal.Storage;

public interface IJournalDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates Journal tables on the shared Postgres database (idempotent).
/// Prefer this over EF <c>EnsureCreated</c>, which skips when other tables already exist.
/// </summary>
public sealed class JournalDatabaseInitializer : IJournalDatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public JournalDatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists journal_entries (
                sequence bigserial primary key,
                id uuid not null,
                type varchar(256) not null,
                published_at timestamptz not null,
                schema_version integer not null,
                publish_policy integer not null,
                payload text null
            );

            create unique index if not exists ix_journal_entries_id
                on journal_entries (id);

            create index if not exists ix_journal_entries_type
                on journal_entries (type, schema_version);

            create index if not exists ix_journal_entries_type_published
                on journal_entries (type, published_at);

            create index if not exists ix_journal_entries_published_at
                on journal_entries (published_at);

            create table if not exists journal_index_keys (
                journal_entry_sequence bigint not null
                    references journal_entries (sequence) on delete cascade,
                type varchar(128) not null,
                value varchar(512) not null,
                primary key (journal_entry_sequence, type)
            );

            create index if not exists ix_journal_index_keys_lookup
                on journal_index_keys (type, value);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class JournalDatabaseInitializerExtensions
{
    public static async Task InitializeJournalDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IJournalDatabaseInitializer>();
        await initializer.InitializeAsync();
    }
}
