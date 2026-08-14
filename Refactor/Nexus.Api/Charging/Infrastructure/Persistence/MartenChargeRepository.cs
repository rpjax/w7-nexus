using Marten;
using Marten.Events.Projections;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Domain.Events;
using Refactor.Nexus.Api.Infrastructure.EventSourcing;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;

namespace Refactor.Nexus.Api.Charging.Infrastructure.Persistence;

public sealed class MartenChargeRepository : IChargeRepository
{
    private readonly IDocumentStore _store;

    public MartenChargeRepository(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<ChargeAggregate?> GetByIdAsync(Guid chargeId, CancellationToken cancellationToken = default)
    {
        var charge = await MartenLiveQuery.LoadAsync<ChargeAggregate>(
            _store, EventStoreStreams.Charge(chargeId), cancellationToken);
        if (charge is null || charge.Id == Guid.Empty)
            return null;
        return charge;
    }

    public async Task<ChargeAggregate?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        var all = await MartenLiveQuery.ListAsync<ChargeAggregate>(_store, "charge-", cancellationToken);
        return all.FirstOrDefault(c => c.ExternalReference == externalReference);
    }

    public async Task SaveAsync(ChargeAggregate charge, CancellationToken cancellationToken = default)
    {
        var events = charge.UncommittedEvents.ToArray();
        if (events.Length == 0)
            return;

        await using var session = _store.LightweightSession();
        await MartenStreamWriter.SaveAsync(
            session,
            EventStoreStreams.Charge(charge.Id),
            typeof(ChargeAggregate),
            events,
            cancellationToken);
        charge.ClearUncommitted();
    }

    public async Task<IReadOnlyList<ChargeAggregate>> ListAsync(
        Guid? operationId,
        Guid? operatorMemberId,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        var all = await MartenLiveQuery.ListAsync<ChargeAggregate>(_store, "charge-", cancellationToken);
        IEnumerable<ChargeAggregate> items = all;
        if (operationId is not null)
            items = items.Where(c => c.OperationId == operationId.Value);
        if (operatorMemberId is not null)
            items = items.Where(c => c.OperatorMemberId == operatorMemberId.Value);
        return items.OrderByDescending(c => c.OpenedAt).ToList();
    }

    public static void Configure(StoreOptions options)
    {
        options.Events.AddEventTypes(
        [
            typeof(ChargeOpened),
            typeof(ChargeExternalReferenceAssigned),
            typeof(ChargePaid),
            typeof(ChargeCancelled),
            typeof(ChargeExpired),
            typeof(ChargeFailed),
            typeof(ChargeMaterialized),
            typeof(ChargeReversed)
        ]);
    }
}
