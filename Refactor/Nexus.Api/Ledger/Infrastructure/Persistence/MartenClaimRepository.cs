using Marten;
using Marten.Events.Projections;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Domain.Events;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using HopAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop.Hop;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;

public sealed class MartenClaimRepository : IClaimRepository, IHopRepository, ILedgerCommit
{
    private readonly IDocumentStore _store;

    public MartenClaimRepository(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<ClaimAggregate?> GetByIdAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        var claim = await session.Events.AggregateStreamAsync<ClaimAggregate>(
            EventStoreStreams.Claim(claimId), token: cancellationToken);
        if (claim is null || claim.Id == Guid.Empty)
            return null;
        return claim;
    }

    public async Task<IReadOnlyList<ClaimAggregate>> ListAsync(
        Guid? originChargeId,
        Guid? locationAccountId,
        Guid? beneficiaryId,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        IEnumerable<ClaimAggregate> items = await session.Query<ClaimAggregate>().ToListAsync(cancellationToken);
        items = items.Where(c => c.Id != Guid.Empty);
        if (originChargeId is not null)
            items = items.Where(c => c.OriginChargeId == originChargeId);
        if (locationAccountId is not null)
            items = items.Where(c => c.LocationAccountId == locationAccountId);
        if (beneficiaryId is not null)
            items = items.Where(c => c.BeneficiaryId == beneficiaryId);
        return items.OrderBy(c => c.OpenedAt).ToList();
    }

    async Task<HopAggregate?> IHopRepository.GetByIdAsync(Guid hopId, CancellationToken cancellationToken)
    {
        await using var session = _store.LightweightSession();
        var hop = await session.Events.AggregateStreamAsync<HopAggregate>(
            EventStoreStreams.Hop(hopId), token: cancellationToken);
        if (hop is null || hop.Id == Guid.Empty)
            return null;
        return hop;
    }

    public async Task<IReadOnlyList<HopAggregate>> ListAsync(
        Guid? originAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        IEnumerable<HopAggregate> items = await session.Query<HopAggregate>().ToListAsync(cancellationToken);
        items = items.Where(h => h.Id != Guid.Empty);
        if (originAccountId is not null)
            items = items.Where(h => h.OriginAccountId == originAccountId);
        return items.OrderByDescending(h => h.OccurredAt).ToList();
    }

    public async Task SaveAsync(
        IReadOnlyList<WorldAccountAggregate> accounts,
        IReadOnlyList<ClaimAggregate> claims,
        HopAggregate? hop = null,
        ChargeAggregate? charge = null,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        if (charge is not null)
        {
            await MartenStreamWriter.QueueAsync(
                session,
                EventStoreStreams.Charge(charge.Id),
                typeof(ChargeAggregate),
                charge.UncommittedEvents,
                cancellationToken);
        }

        foreach (var account in accounts)
        {
            await MartenStreamWriter.QueueAsync(
                session,
                EventStoreStreams.WorldAccount(account.Id),
                typeof(WorldAccountAggregate),
                account.UncommittedEvents,
                cancellationToken);
        }

        foreach (var claim in claims)
        {
            await MartenStreamWriter.QueueAsync(
                session,
                EventStoreStreams.Claim(claim.Id),
                typeof(ClaimAggregate),
                claim.UncommittedEvents,
                cancellationToken);
        }

        if (hop is not null)
        {
            await MartenStreamWriter.QueueAsync(
                session,
                EventStoreStreams.Hop(hop.Id),
                typeof(HopAggregate),
                hop.UncommittedEvents,
                cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
        charge?.ClearUncommitted();
        foreach (var account in accounts)
            account.ClearUncommitted();
        foreach (var claim in claims)
            claim.ClearUncommitted();
        hop?.ClearUncommitted();
    }

    public static void Configure(StoreOptions options)
    {
        options.Projections.Snapshot<ClaimAggregate>(SnapshotLifecycle.Inline);
        options.Schema.For<ClaimAggregate>().Identity(x => x.Id);
        options.Projections.Snapshot<HopAggregate>(SnapshotLifecycle.Inline);
        options.Schema.For<HopAggregate>().Identity(x => x.Id);
        options.Events.AddEventTypes(
        [
            typeof(ClaimOpened),
            typeof(ClaimAdjusted),
            typeof(ClaimArchived),
            typeof(ClaimRepassed),
            typeof(HopRegistered)
        ]);
    }
}
