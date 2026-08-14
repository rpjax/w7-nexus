using Npgsql;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;

namespace Refactor.Nexus.Api.Mandates.Infrastructure.Persistence;

public interface IMandatesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class MandatesDatabaseInitializer : IMandatesDatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public MandatesDatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists member_mandate_presets (
                account_id uuid not null,
                preset_id varchar(64) not null,
                primary key (account_id, preset_id)
            );

            create table if not exists member_mandate_grants (
                id uuid primary key,
                account_id uuid not null,
                capability varchar(128) not null,
                scope_json text not null,
                granted_by uuid not null,
                granted_at timestamptz not null,
                source_preset varchar(64) null
            );

            create index if not exists ix_member_mandate_grants_account
                on member_mandate_grants (account_id);

            create index if not exists ix_member_mandate_grants_granted_by
                on member_mandate_grants (granted_by);

            create table if not exists agency_deals (
                id uuid primary key,
                recruiter_id uuid not null,
                operator_id uuid not null,
                operator_percent numeric(7,4) not null,
                recruiter_percent numeric(7,4) not null,
                status varchar(16) not null,
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );

            create unique index if not exists ix_agency_deals_active_operator
                on agency_deals (operator_id)
                where status = 'Active';

            create table if not exists shareholder_stakes (
                account_id uuid primary key,
                percentage numeric(7,4) not null,
                created_at timestamptz not null,
                last_updated_at timestamptz not null
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class MandatesDatabaseInitializerExtensions
{
    public static async Task InitializeMandatesDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IMandatesDatabaseInitializer>();
        await initializer.InitializeAsync();
        await scope.ServiceProvider.GetRequiredService<MartenMandateRepositories>().BackfillLegacyAsync();
    }
}

public sealed class PostgresMemberMandateRepository : IMemberMandateRepository, IMemberMandateReadRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresMemberMandateRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MemberMandate?> GetByMemberIdAsync(MemberId memberId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var presets = new List<string>();
        await using (var presetCmd = connection.CreateCommand())
        {
            presetCmd.CommandText = "select preset_id from member_mandate_presets where account_id = @id";
            presetCmd.Parameters.AddWithValue("id", memberId.Value);
            await using var reader = await presetCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                presets.Add(reader.GetString(0));
        }

        var grants = new List<MandateGrant>();
        await using (var grantCmd = connection.CreateCommand())
        {
            grantCmd.CommandText = """
                select id, capability, scope_json, granted_by, granted_at, source_preset
                from member_mandate_grants
                where account_id = @id
                """;
            grantCmd.Parameters.AddWithValue("id", memberId.Value);
            await using var reader = await grantCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                grants.Add(new MandateGrant(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    MandateScope.FromStorageJson(reader.GetString(2)),
                    new MemberId(reader.GetGuid(3)),
                    reader.GetDateTime(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5)));
            }
        }

        if (presets.Count == 0 && grants.Count == 0)
            return null;

        return MemberMandate.Rehydrate(memberId, grants, presets);
    }

    public async Task SaveAsync(MemberMandate mandate, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var deletePresets = connection.CreateCommand())
        {
            deletePresets.Transaction = tx;
            deletePresets.CommandText = "delete from member_mandate_presets where account_id = @id";
            deletePresets.Parameters.AddWithValue("id", mandate.MemberId.Value);
            await deletePresets.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteGrants = connection.CreateCommand())
        {
            deleteGrants.Transaction = tx;
            deleteGrants.CommandText = "delete from member_mandate_grants where account_id = @id";
            deleteGrants.Parameters.AddWithValue("id", mandate.MemberId.Value);
            await deleteGrants.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var preset in mandate.AppliedPresets)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                insert into member_mandate_presets (account_id, preset_id)
                values (@account_id, @preset_id)
                """;
            insert.Parameters.AddWithValue("account_id", mandate.MemberId.Value);
            insert.Parameters.AddWithValue("preset_id", preset);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var grant in mandate.Grants)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                insert into member_mandate_grants
                    (id, account_id, capability, scope_json, granted_by, granted_at, source_preset)
                values
                    (@id, @account_id, @capability, @scope_json, @granted_by, @granted_at, @source_preset)
                """;
            insert.Parameters.AddWithValue("id", grant.Id);
            insert.Parameters.AddWithValue("account_id", mandate.MemberId.Value);
            insert.Parameters.AddWithValue("capability", grant.Capability);
            insert.Parameters.AddWithValue("scope_json", grant.Scope.ToStorageJson());
            insert.Parameters.AddWithValue("granted_by", grant.GrantedBy.Value);
            insert.Parameters.AddWithValue("granted_at", grant.GrantedAt);
            insert.Parameters.AddWithValue("source_preset", (object?)grant.SourcePreset ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemberMandate>> ListGrantedByAsync(MemberId grantorId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select distinct account_id from member_mandate_grants where granted_by = @id";
        command.Parameters.AddWithValue("id", grantorId.Value);

        var accountIds = new List<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                accountIds.Add(reader.GetGuid(0));
        }

        var result = new List<MemberMandate>();
        foreach (var id in accountIds)
        {
            var mandate = await GetByMemberIdAsync(new MemberId(id), cancellationToken);
            if (mandate is not null)
                result.Add(mandate);
        }

        return result;
    }

    public async Task<IReadOnlyList<MemberMandate>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select account_id from member_mandate_presets
            union
            select account_id from member_mandate_grants
            """;

        var accountIds = new List<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                accountIds.Add(reader.GetGuid(0));
        }

        var result = new List<MemberMandate>();
        foreach (var id in accountIds.Distinct())
        {
            var mandate = await GetByMemberIdAsync(new MemberId(id), cancellationToken);
            if (mandate is not null)
                result.Add(mandate);
        }

        return result;
    }
}

public sealed class PostgresAgencyDealRepository : IAgencyDealRepository, IAgencyDealReadRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresAgencyDealRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AgencyDealAggregate?> GetActiveByOperatorIdAsync(MemberId operatorId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at
            from agency_deals
            where operator_id = @operator_id and status = 'Active'
            limit 1
            """;
        command.Parameters.AddWithValue("operator_id", operatorId.Value);
        return await ReadDealAsync(command, cancellationToken);
    }

    public async Task<AgencyDealAggregate?> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at
            from agency_deals
            where id = @id
            """;
        command.Parameters.AddWithValue("id", dealId);
        return await ReadDealAsync(command, cancellationToken);
    }

    public async Task SaveAsync(AgencyDealAggregate deal, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into agency_deals
                (id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at)
            values
                (@id, @recruiter_id, @operator_id, @operator_percent, @recruiter_percent, @status, @created_at, @last_updated_at)
            on conflict (id) do update set
                recruiter_id = excluded.recruiter_id,
                operator_percent = excluded.operator_percent,
                recruiter_percent = excluded.recruiter_percent,
                status = excluded.status,
                last_updated_at = excluded.last_updated_at
            """;
        command.Parameters.AddWithValue("id", deal.Id);
        command.Parameters.AddWithValue("recruiter_id", deal.RecruiterId.Value);
        command.Parameters.AddWithValue("operator_id", deal.OperatorId.Value);
        command.Parameters.AddWithValue("operator_percent", deal.OperatorPercent);
        command.Parameters.AddWithValue("recruiter_percent", deal.RecruiterPercent);
        command.Parameters.AddWithValue("status", deal.Status.ToString());
        command.Parameters.AddWithValue("created_at", deal.CreatedAt);
        command.Parameters.AddWithValue("last_updated_at", deal.LastUpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasActiveDealForOperatorAsync(MemberId operatorId, CancellationToken cancellationToken = default) =>
        await GetActiveByOperatorIdAsync(operatorId, cancellationToken) is not null;

    public async Task<IReadOnlyList<AgencyDealAggregate>> ListActiveByRecruiterAsync(MemberId recruiterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at
            from agency_deals
            where recruiter_id = @recruiter_id and status = 'Active'
            """;
        command.Parameters.AddWithValue("recruiter_id", recruiterId.Value);
        return await ReadDealsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AgencyDealAggregate>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at
            from agency_deals
            where status = 'Active'
            """;
        return await ReadDealsAsync(command, cancellationToken);
    }

    private static async Task<AgencyDealAggregate?> ReadDealAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return MapDeal(reader);
    }

    private static async Task<IReadOnlyList<AgencyDealAggregate>> ReadDealsAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var items = new List<AgencyDealAggregate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(MapDeal(reader));
        return items;
    }

    private static AgencyDealAggregate MapDeal(NpgsqlDataReader reader) =>
        AgencyDealAggregate.Rehydrate(
            reader.GetGuid(0),
            new MemberId(reader.GetGuid(1)),
            new MemberId(reader.GetGuid(2)),
            reader.GetDecimal(3),
            reader.GetDecimal(4),
            Enum.Parse<AgencyDealStatus>(reader.GetString(5), ignoreCase: true),
            reader.GetDateTime(6),
            reader.GetDateTime(7));
}

public sealed class PostgresShareholderStakeRepository : IShareholderStakeRepository, IShareholderStakeReadRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresShareholderStakeRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ShareholderStakeAggregate?> GetByAccountIdAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select account_id, percentage, created_at, last_updated_at
            from shareholder_stakes
            where account_id = @id
            """;
        command.Parameters.AddWithValue("id", accountId.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return ShareholderStakeAggregate.Rehydrate(
            new MemberId(reader.GetGuid(0)),
            reader.GetDecimal(1),
            reader.GetDateTime(2),
            reader.GetDateTime(3));
    }

    public async Task SaveAsync(ShareholderStakeAggregate stake, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into shareholder_stakes (account_id, percentage, created_at, last_updated_at)
            values (@account_id, @percentage, @created_at, @last_updated_at)
            on conflict (account_id) do update set
                percentage = excluded.percentage,
                last_updated_at = excluded.last_updated_at
            """;
        command.Parameters.AddWithValue("account_id", stake.AccountId.Value);
        command.Parameters.AddWithValue("percentage", stake.Percentage);
        command.Parameters.AddWithValue("created_at", stake.CreatedAt);
        command.Parameters.AddWithValue("last_updated_at", stake.LastUpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "delete from shareholder_stakes where account_id = @id";
        command.Parameters.AddWithValue("id", accountId.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShareholderStakeAggregate>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select account_id, percentage, created_at, last_updated_at
            from shareholder_stakes
            order by percentage desc
            """;
        var items = new List<ShareholderStakeAggregate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ShareholderStakeAggregate.Rehydrate(
                new MemberId(reader.GetGuid(0)),
                reader.GetDecimal(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3)));
        }

        return items;
    }

    public async Task<decimal> SumPercentagesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select coalesce(sum(percentage), 0) from shareholder_stakes";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result is DBNull ? 0m : Convert.ToDecimal(result);
    }
}
