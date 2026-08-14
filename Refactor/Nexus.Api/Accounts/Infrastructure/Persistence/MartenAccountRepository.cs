using Marten;
using Marten.Events.Projections;
using Npgsql;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Events;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using AccountAggregate = Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account.Account;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;

public sealed class MartenAccountRepository : IAccountRepository, IAccountReadRepository
{
    private readonly IDocumentStore _store;
    private readonly INpgsqlConnectionFactory _connections;

    public MartenAccountRepository(IDocumentStore store, INpgsqlConnectionFactory connections)
    {
        _store = store;
        _connections = connections;
    }

    public async Task<AccountAggregate?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        var account = await MartenLiveQuery.LoadAsync<AccountAggregate>(
            _store, EventStoreStreams.Account(accountId.Value), cancellationToken);
        if (account is null || account.Id.Value == Guid.Empty)
            return null;
        await AttachSecretAsync(account, cancellationToken);
        return account;
    }

    public async Task<AccountAggregate> CreateAsync(AccountAggregate account, CancellationToken cancellationToken = default)
    {
        await SaveStreamAsync(account, cancellationToken);
        await UpsertSecretAsync(account.Id.Value, account.PasswordHash, cancellationToken);
        await UpsertUsernameIndexAsync(account.Id.Value, account.Username, cancellationToken);
        return account;
    }

    public async Task UpdateAsync(AccountAggregate account, CancellationToken cancellationToken = default)
    {
        await SaveStreamAsync(account, cancellationToken);
        if (!string.IsNullOrWhiteSpace(account.PasswordHash))
            await UpsertSecretAsync(account.Id.Value, account.PasswordHash, cancellationToken);
        await UpsertUsernameIndexAsync(account.Id.Value, account.Username, cancellationToken);
    }

    public async Task UpdateChangingUsernameAsync(
        AccountAggregate account,
        string previousUsername,
        CancellationToken cancellationToken = default)
    {
        await UpdateAsync(account, cancellationToken);
        await RetireUsernameAsync(previousUsername, account.Id, cancellationToken);
    }

    public async Task RetireUsernameAsync(string username, AccountId retiredFrom, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into retired_usernames (username_lower, original_username, retired_from, retired_at)
            values (lower(@username), @original, @retired_from, @retired_at)
            on conflict (username_lower) do nothing
            """;
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("original", username.Trim());
        command.Parameters.AddWithValue("retired_from", retiredFrom.Value);
        command.Parameters.AddWithValue("retired_at", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AccountAggregate?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var match = (await MartenLiveQuery.ListAsync<AccountAggregate>(_store, "account-", cancellationToken))
            .FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return null;
        await AttachSecretAsync(match, cancellationToken);
        return match;
    }

    public async Task<bool> IsUsernameRetiredAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select exists(select 1 from retired_usernames where username_lower = lower(@username))";
        command.Parameters.AddWithValue("username", username);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default) =>
        await FindByUsernameAsync(username, cancellationToken) is not null
        || await IsUsernameRetiredAsync(username, cancellationToken);

    public async Task<(IReadOnlyList<AccountAggregate> Items, int Total)> SearchAsync(
        string? keyword,
        string? status,
        string? role,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AccountAggregate> items = await MartenLiveQuery.ListAsync<AccountAggregate>(
            _store, "account-", cancellationToken);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            items = items.Where(a =>
                a.Username.Contains(term, StringComparison.OrdinalIgnoreCase)
                || a.Id.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
            items = items.Where(a => string.Equals(a.Status.ToString(), status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(role))
            items = items.Where(a => a.Roles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase));

        var list = items.OrderByDescending(a => a.LastUpdatedAt).ToList();
        var total = list.Count;
        return (list.Skip(offset).Take(limit).ToList(), total);
    }

    public async Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var items = await MartenLiveQuery.ListAsync<AccountAggregate>(_store, "account-", cancellationToken);
        return items.Count(a => a.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    public async Task BackfillLegacyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, username, password_hash, status, roles, permissions, created_at, last_updated_at
            from accounts
            """;
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                await using var session = _store.LightweightSession();
                var state = await session.Events.FetchStreamStateAsync(EventStoreStreams.Account(id), cancellationToken);
                if (state is not null)
                    continue;

                var username = reader.GetString(1);
                var hash = reader.GetString(2);
                var status = Enum.TryParse<AccountStatus>(reader.GetString(3), true, out var parsed)
                    ? parsed : AccountStatus.Active;
                var roles = reader.GetFieldValue<string[]>(4);
                var permissions = reader.GetFieldValue<string[]>(5);
                var created = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
                var updated = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc);
                session.Events.StartStream<AccountAggregate>(
                    EventStoreStreams.Account(id),
                    new AccountBackfilled(id, username, status, roles, permissions, created, updated));
                await session.SaveChangesAsync(cancellationToken);
                await UpsertSecretAsync(id, hash, cancellationToken);
                await UpsertUsernameIndexAsync(id, username, cancellationToken);
            }
        }
        catch (PostgresException)
        {
            // legacy table may not exist on a fresh schema that already skipped accounts
        }
    }

    public static void Configure(StoreOptions options)
    {
        options.Events.AddEventTypes(
        [
            typeof(AccountRegistered),
            typeof(AccountBackfilled),
            typeof(AccountDisabled),
            typeof(AccountEnabled),
            typeof(AccountAdministratorGranted),
            typeof(AccountAdministratorRevoked),
            typeof(AccountUsernameChanged),
            typeof(AccountPasswordChanged),
            typeof(AccountPermissionGranted),
            typeof(AccountPermissionRevoked)
        ]);
    }

    private async Task SaveStreamAsync(AccountAggregate account, CancellationToken cancellationToken)
    {
        var events = account.UncommittedEvents.ToArray();
        if (events.Length == 0)
            return;

        await using var session = _store.LightweightSession();
        await MartenStreamWriter.SaveAsync(
            session,
            EventStoreStreams.Account(account.Id.Value),
            typeof(AccountAggregate),
            events,
            cancellationToken);
        account.ClearUncommitted();
    }

    private async Task AttachSecretAsync(AccountAggregate account, CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select password_hash from account_secrets where account_id = @id";
        command.Parameters.AddWithValue("id", account.Id.Value);
        var hash = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (!string.IsNullOrWhiteSpace(hash))
            account.AttachPasswordHash(hash);
    }

    private async Task UpsertSecretAsync(Guid accountId, string passwordHash, CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into account_secrets (account_id, password_hash)
            values (@id, @hash)
            on conflict (account_id) do update set password_hash = excluded.password_hash
            """;
        command.Parameters.AddWithValue("id", accountId);
        command.Parameters.AddWithValue("hash", passwordHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpsertUsernameIndexAsync(Guid accountId, string username, CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            delete from account_usernames where account_id = @id;
            insert into account_usernames (username_lower, account_id)
            values (lower(@username), @id)
            on conflict (username_lower) do update set account_id = excluded.account_id
            """;
        command.Parameters.AddWithValue("id", accountId);
        command.Parameters.AddWithValue("username", username);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
