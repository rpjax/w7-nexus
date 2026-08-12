using Npgsql;
using NpgsqlTypes;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence.ReadModels;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence.Records;
using Refactor.Nexus.Api.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Persistence.Repositories;

public sealed class PostgresAccountRepository : IAccountRepository, IAccountReadRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresAccountRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Account?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, username, password_hash, status, roles, permissions, created_at, last_updated_at
            from accounts
            where id = @id;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", accountId.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ToAccount(ToRecord(reader));
    }

    public async Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into accounts (id, username, password_hash, status, roles, permissions, created_at, last_updated_at)
            values (@id, @username, @password_hash, @status, @roles, @permissions, @created_at, @last_updated_at);
            """;

        var record = ToRecord(account);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        BindRecord(command, record);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return account;
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update accounts
            set username = @username,
                password_hash = @password_hash,
                status = @status,
                roles = @roles,
                permissions = @permissions,
                last_updated_at = @last_updated_at
            where id = @id;
            """;

        var record = ToRecord(account);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        BindRecord(command, record, includeCreatedAt: false);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, username, password_hash, status, roles, permissions, created_at, last_updated_at
            from accounts
            where lower(username) = lower(@username)
            limit 1;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ToAccount(ToRecord(reader));
    }

    public async Task<(IReadOnlyList<Account> Items, int Total)> SearchAsync(
        string? keyword,
        string? status,
        string? role,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";
        var statusFilter = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var roleFilter = string.IsNullOrWhiteSpace(role) ? null : role.Trim();

        const string countSql = """
            select count(*)
            from accounts
            where (@term is null or cast(id as text) ilike @term or username ilike @term)
              and (@status is null or lower(status) = lower(@status))
              and (
                @role is null
                or exists (
                    select 1
                    from unnest(roles) as assigned_role
                    where lower(assigned_role) = lower(@role)
                )
              );
            """;

        const string dataSql = """
            select id, username, status, roles, permissions, created_at, last_updated_at
            from accounts
            where (@term is null or cast(id as text) ilike @term or username ilike @term)
              and (@status is null or lower(status) = lower(@status))
              and (
                @role is null
                or exists (
                    select 1
                    from unnest(roles) as assigned_role
                    where lower(assigned_role) = lower(@role)
                )
              )
            order by last_updated_at desc
            offset @offset
            limit @limit;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var countCommand = new NpgsqlCommand(countSql, connection);
        BindSearchFilters(countCommand, term, statusFilter, roleFilter);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = new NpgsqlCommand(dataSql, connection);
        BindSearchFilters(dataCommand, term, statusFilter, roleFilter);
        dataCommand.Parameters.AddWithValue("offset", offset);
        dataCommand.Parameters.AddWithValue("limit", limit);

        var items = new List<Account>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ToAccount(ToReadModel(reader)));
        }

        return (items, total);
    }

    public async Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select count(*)
            from accounts
            where exists (
                select 1
                from unnest(roles) as assigned_role
                where lower(assigned_role) = lower(@role)
            );
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static void BindRecord(NpgsqlCommand command, AccountRecord record, bool includeCreatedAt = true)
    {
        command.Parameters.AddWithValue("id", record.Id);
        command.Parameters.AddWithValue("username", record.Username);
        command.Parameters.AddWithValue("password_hash", record.PasswordHash);
        command.Parameters.AddWithValue("status", record.Status);
        command.Parameters.AddWithValue("roles", record.Roles);
        command.Parameters.AddWithValue("permissions", record.Permissions);

        if (includeCreatedAt)
            command.Parameters.AddWithValue("created_at", record.CreatedAt);

        command.Parameters.AddWithValue("last_updated_at", record.LastUpdatedAt);
    }

    private static AccountRecord ToRecord(Account account) =>
        new()
        {
            Id = account.Id.Value,
            Username = account.Username,
            PasswordHash = account.PasswordHash,
            Status = account.Status.ToString(),
            Roles = account.Roles.ToArray(),
            Permissions = account.Permissions.ToArray(),
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };

    private static AccountRecord ToRecord(NpgsqlDataReader reader) =>
        new()
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Status = reader.GetString(3),
            Roles = reader.GetFieldValue<string[]>(4),
            Permissions = reader.GetFieldValue<string[]>(5),
            CreatedAt = reader.GetFieldValue<DateTime>(6),
            LastUpdatedAt = reader.GetFieldValue<DateTime>(7)
        };

    private static NpgsqlParameter CreateSearchTermParameter(string? term) =>
        new("term", NpgsqlDbType.Text)
        {
            Value = term is null ? DBNull.Value : term,
        };

    private static void BindSearchFilters(
        NpgsqlCommand command,
        string? term,
        string? status,
        string? role)
    {
        command.Parameters.Add(CreateSearchTermParameter(term));
        command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text)
        {
            Value = status is null ? DBNull.Value : status,
        });
        command.Parameters.Add(new NpgsqlParameter("role", NpgsqlDbType.Text)
        {
            Value = role is null ? DBNull.Value : role,
        });
    }

    private static AccountReadModel ToReadModel(NpgsqlDataReader reader) =>
        new()
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            Status = reader.GetString(2),
            Roles = reader.GetFieldValue<string[]>(3),
            Permissions = reader.GetFieldValue<string[]>(4),
            CreatedAt = reader.GetFieldValue<DateTime>(5),
            LastUpdatedAt = reader.GetFieldValue<DateTime>(6)
        };

    private static AccountStatus ParseStatus(string status) =>
        Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : AccountStatus.Active;

    private static Account ToAccount(AccountRecord record) =>
        Account.Rehydrate(
            new AccountId(record.Id),
            record.Username,
            record.PasswordHash,
            ParseStatus(record.Status),
            record.Roles,
            record.Permissions,
            DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(record.LastUpdatedAt, DateTimeKind.Utc));

    private static Account ToAccount(AccountReadModel readModel) =>
        Account.Rehydrate(
            new AccountId(readModel.Id),
            readModel.Username,
            string.Empty,
            ParseStatus(readModel.Status),
            readModel.Roles,
            readModel.Permissions,
            DateTime.SpecifyKind(readModel.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(readModel.LastUpdatedAt, DateTimeKind.Utc));
}
