using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Events;

namespace Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop;

public sealed class Hop
{
    private readonly List<object> _uncommitted = [];

    public Hop()
    {
    }

    public Guid Id { get; private set; }
    public Guid OriginAccountId { get; private set; }
    public string OriginCurrency { get; private set; } = "BRL";
    public Guid[] BundleClaimIds { get; private set; } = [];
    public HopDestinationSnapshot[] Destinations { get; private set; } = [];
    public Guid? CutOrangeMemberId { get; private set; }
    public decimal? CutPercent { get; private set; }
    public bool CutInPlace { get; private set; }
    public decimal LossAmount { get; private set; }
    public DateTime OccurredAt { get; private set; }

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public static Hop Register(
        Guid originAccountId,
        string originCurrency,
        IReadOnlyList<Guid> bundleClaimIds,
        IReadOnlyList<HopDestinationSnapshot> destinations,
        Guid? cutOrangeMemberId,
        decimal? cutPercent,
        bool cutInPlace,
        decimal lossAmount)
    {
        var hop = new Hop();
        hop.ApplyChange(new HopRegistered(
            Guid.NewGuid(),
            originAccountId,
            originCurrency,
            bundleClaimIds.ToArray(),
            destinations.ToArray(),
            cutOrangeMemberId,
            cutPercent,
            cutInPlace,
            lossAmount,
            DateTime.UtcNow));
        return hop;
    }

    public void ClearUncommitted() => _uncommitted.Clear();

    public void Apply(HopRegistered e)
    {
        Id = e.HopId;
        OriginAccountId = e.OriginAccountId;
        OriginCurrency = e.OriginCurrency;
        BundleClaimIds = e.BundleClaimIds;
        Destinations = e.Destinations;
        CutOrangeMemberId = e.CutOrangeMemberId;
        CutPercent = e.CutPercent;
        CutInPlace = e.CutInPlace;
        LossAmount = e.LossAmount;
        OccurredAt = e.OccurredAt;
    }

    private void ApplyChange(object @event)
    {
        if (@event is HopRegistered registered)
            Apply(registered);
        else
            throw new InvalidOperationException(@event.GetType().Name);

        _uncommitted.Add(@event);
    }
}
