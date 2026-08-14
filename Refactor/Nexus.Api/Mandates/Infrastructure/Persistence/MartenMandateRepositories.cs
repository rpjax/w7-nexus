using Marten;
using Marten.Events.Projections;
using Npgsql;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Events;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;
using MemberMandateAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate.MemberMandate;

namespace Refactor.Nexus.Api.Mandates.Infrastructure.Persistence;

public sealed class MartenMandateRepositories :
    IMemberMandateRepository,
    IMemberMandateReadRepository,
    IAgencyDealRepository,
    IAgencyDealReadRepository,
    IShareholderStakeRepository,
    IShareholderStakeReadRepository
{
    private readonly IDocumentStore _store;
    private readonly INpgsqlConnectionFactory _connections;

    public MartenMandateRepositories(IDocumentStore store, INpgsqlConnectionFactory connections)
    {
        _store = store;
        _connections = connections;
    }

    public async Task<MemberMandateAggregate?> GetByMemberIdAsync(
        MemberId memberId,
        CancellationToken cancellationToken = default)
    {
        var mandate = await MartenLiveQuery.LoadAsync<MemberMandateAggregate>(
            _store, EventStoreStreams.Mandate(memberId.Value), cancellationToken);
        if (mandate is null || mandate.MemberId.Value == Guid.Empty)
            return null;
        return mandate;
    }

    public async Task SaveAsync(MemberMandateAggregate mandate, CancellationToken cancellationToken = default)
    {
        await SaveStreamAsync(
            EventStoreStreams.Mandate(mandate.MemberId.Value),
            typeof(MemberMandateAggregate),
            mandate.UncommittedEvents,
            cancellationToken);
        mandate.ClearUncommitted();
    }

    public async Task<IReadOnlyList<MemberMandateAggregate>> ListGrantedByAsync(
        MemberId grantorId,
        CancellationToken cancellationToken = default)
    {
        var all = await ((IMemberMandateReadRepository)this).ListAllAsync(cancellationToken);
        return all.Where(m => m.Grants.Any(g => g.GrantedBy.Equals(grantorId))).ToList();
    }

    async Task<IReadOnlyList<MemberMandateAggregate>> IMemberMandateReadRepository.ListAllAsync(
        CancellationToken cancellationToken)
    {
        await using var session = _store.QuerySession();
        var items = await MartenLiveQuery.ListAsync<MemberMandateAggregate>(_store, "mandate-", cancellationToken);
        return items.Where(m => m.MemberId.Value != Guid.Empty).ToList();
    }

    public async Task<AgencyDealAggregate?> GetActiveByOperatorIdAsync(
        MemberId operatorId,
        CancellationToken cancellationToken = default)
    {
        var deals = await ListDealsAsync(cancellationToken);
        return deals.FirstOrDefault(d => d.IsActive && d.OperatorId.Equals(operatorId));
    }

    public async Task<AgencyDealAggregate?> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await MartenLiveQuery.LoadAsync<AgencyDealAggregate>(
            _store, EventStoreStreams.Deal(dealId), cancellationToken);
        if (deal is null || deal.Id == Guid.Empty)
            return null;
        return deal;
    }

    public async Task SaveAsync(AgencyDealAggregate deal, CancellationToken cancellationToken = default)
    {
        await SaveStreamAsync(
            EventStoreStreams.Deal(deal.Id),
            typeof(AgencyDealAggregate),
            deal.UncommittedEvents,
            cancellationToken);
        deal.ClearUncommitted();
    }

    public async Task<bool> HasActiveDealForOperatorAsync(MemberId operatorId, CancellationToken cancellationToken = default) =>
        await GetActiveByOperatorIdAsync(operatorId, cancellationToken) is not null;

    public async Task<IReadOnlyList<AgencyDealAggregate>> ListActiveByRecruiterAsync(
        MemberId recruiterId,
        CancellationToken cancellationToken = default)
    {
        var deals = await ListDealsAsync(cancellationToken);
        return deals.Where(d => d.IsActive && d.RecruiterId.Equals(recruiterId)).ToList();
    }

    public async Task<IReadOnlyList<AgencyDealAggregate>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var deals = await ListDealsAsync(cancellationToken);
        return deals.Where(d => d.IsActive).ToList();
    }

    public async Task<ShareholderStakeAggregate?> GetByAccountIdAsync(
        MemberId accountId,
        CancellationToken cancellationToken = default)
    {
        var stake = await MartenLiveQuery.LoadAsync<ShareholderStakeAggregate>(
            _store, EventStoreStreams.Stake(accountId.Value), cancellationToken);
        if (stake is null || stake.AccountId.Value == Guid.Empty || stake.IsRemoved)
            return null;
        return stake;
    }

    public async Task SaveAsync(ShareholderStakeAggregate stake, CancellationToken cancellationToken = default)
    {
        await SaveStreamAsync(
            EventStoreStreams.Stake(stake.AccountId.Value),
            typeof(ShareholderStakeAggregate),
            stake.UncommittedEvents,
            cancellationToken);
        stake.ClearUncommitted();
    }

    public async Task DeleteAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByAccountIdAsync(accountId, cancellationToken);
        if (existing is null)
            return;
        existing.Remove();
        await SaveAsync(existing, cancellationToken);
    }

    async Task<IReadOnlyList<ShareholderStakeAggregate>> IShareholderStakeReadRepository.ListAllAsync(
        CancellationToken cancellationToken)
    {
        await using var session = _store.QuerySession();
        var items = await MartenLiveQuery.ListAsync<ShareholderStakeAggregate>(_store, "stake-", cancellationToken);
        return items.Where(s => s.AccountId.Value != Guid.Empty && !s.IsRemoved).ToList();
    }

    public async Task<decimal> SumPercentagesAsync(CancellationToken cancellationToken = default)
    {
        var items = await ((IShareholderStakeReadRepository)this).ListAllAsync(cancellationToken);
        return items.Sum(s => s.Percentage);
    }

    public async Task BackfillLegacyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await BackfillMandatesAsync(cancellationToken);
            await BackfillDealsAsync(cancellationToken);
            await BackfillStakesAsync(cancellationToken);
        }
        catch (PostgresException)
        {
        }
    }

    public static void Configure(StoreOptions options)
    {
        options.Events.AddEventTypes(
        [
            typeof(MandateOpened),
            typeof(MandateBackfilled),
            typeof(MandatePresetGranted),
            typeof(MandatePresetRevoked),
            typeof(MandateCapabilityGranted),
            typeof(MandateCapabilityRevoked),
            typeof(MandateGrantsPruned),
            typeof(MandateGrantsReparented),
            typeof(MemberAttritionRecorded),
            typeof(AgencyDealOpened),
            typeof(AgencyDealBackfilled),
            typeof(AgencyDealRatesChanged),
            typeof(AgencyDealClosed),
            typeof(ShareholderStakeSet),
            typeof(ShareholderStakeRemoved)
        ]);
    }

    private async Task<IReadOnlyList<AgencyDealAggregate>> ListDealsAsync(CancellationToken cancellationToken)
    {
        await using var session = _store.QuerySession();
        var items = await MartenLiveQuery.ListAsync<AgencyDealAggregate>(_store, "deal-", cancellationToken);
        return items.Where(d => d.Id != Guid.Empty).ToList();
    }

    private async Task SaveStreamAsync(
        string streamKey,
        Type aggregateType,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return;
        await using var session = _store.LightweightSession();
        await MartenStreamWriter.SaveAsync(session, streamKey, aggregateType, events, cancellationToken);
    }

    private async Task BackfillMandatesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select account_id from member_mandate_presets
            union
            select account_id from member_mandate_grants
            """;
        var ids = new List<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                ids.Add(reader.GetGuid(0));
        }

        foreach (var id in ids.Distinct())
        {
            await using var session = _store.LightweightSession();
            var key = EventStoreStreams.Mandate(id);
            if (await session.Events.FetchStreamStateAsync(key, cancellationToken) is not null)
                continue;

            var presets = new List<string>();
            await using (var presetCmd = connection.CreateCommand())
            {
                presetCmd.CommandText = "select preset_id from member_mandate_presets where account_id = @id";
                presetCmd.Parameters.AddWithValue("id", id);
                await using var reader = await presetCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    presets.Add(reader.GetString(0));
            }

            var grants = new List<MandateGrantDto>();
            await using (var grantCmd = connection.CreateCommand())
            {
                grantCmd.CommandText = """
                    select id, capability, scope_json, granted_by, granted_at, source_preset
                    from member_mandate_grants where account_id = @id
                    """;
                grantCmd.Parameters.AddWithValue("id", id);
                await using var reader = await grantCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    grants.Add(new MandateGrantDto(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetGuid(3),
                        DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                        reader.IsDBNull(5) ? null : reader.GetString(5)));
                }
            }

            session.Events.StartStream<MemberMandateAggregate>(key, new MandateBackfilled(id, grants.ToArray(), presets.ToArray()));
            await session.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task BackfillDealsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, recruiter_id, operator_id, operator_percent, recruiter_percent, status, created_at, last_updated_at
            from agency_deals
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            await using var session = _store.LightweightSession();
            var key = EventStoreStreams.Deal(id);
            if (await session.Events.FetchStreamStateAsync(key, cancellationToken) is not null)
                continue;
            session.Events.StartStream<AgencyDealAggregate>(
                key,
                new AgencyDealBackfilled(
                    id,
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetString(5),
                    DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
                    DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
            await session.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task BackfillStakesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select account_id, percentage, created_at, last_updated_at from shareholder_stakes";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            await using var session = _store.LightweightSession();
            var key = EventStoreStreams.Stake(id);
            if (await session.Events.FetchStreamStateAsync(key, cancellationToken) is not null)
                continue;
            var occurred = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc);
            session.Events.StartStream<ShareholderStakeAggregate>(
                key,
                new ShareholderStakeSet(id, reader.GetDecimal(1), occurred, null));
            await session.SaveChangesAsync(cancellationToken);
        }
    }
}
