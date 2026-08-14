using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Charging.Domain.Events;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;

namespace Refactor.Nexus.Api.Tests;

public sealed class ChargeDomainTests
{
    [Fact]
    public void Second_paid_does_not_append_event()
    {
        var charge = OpenSample();
        Assert.True(charge.MarkPaid().IsSuccess);
        var count = charge.UncommittedEvents.Count;
        Assert.True(charge.MarkPaid().IsSuccess);
        Assert.Equal(count, charge.UncommittedEvents.Count);
        Assert.Single(charge.UncommittedEvents.OfType<ChargePaid>());
    }

    [Fact]
    public void Paid_cannot_cancel()
    {
        var charge = OpenSample();
        Assert.True(charge.MarkPaid().IsSuccess);
        var cancelled = charge.Cancel();
        Assert.True(cancelled.IsFailure);
        Assert.Equal(ChargingErrorCodes.AlreadyPaid, cancelled.Errors.First().Code);
    }

    [Fact]
    public void Waterfall_has_five_lines_in_order()
    {
        var intent = SplitIntentFactory.Create(
            Guid.NewGuid(),
            10,
            [new ShareholderSlice(Guid.NewGuid(), 20), new ShareholderSlice(Guid.NewGuid(), 10)],
            5,
            new AgencySlice(Guid.NewGuid(), 60, Guid.NewGuid(), 20));

        Assert.True(intent.IsSuccess);
        Assert.Equal(5, intent.Value!.Lines.Count);
        Assert.Equal("Orange", intent.Value.Lines[0].Kind);
        Assert.Equal("ResidualOrg", intent.Value.Lines[4].Kind);
        Assert.Equal(10m, intent.Value.Lines[0].PercentOfRemainder);
        Assert.Equal(30m, intent.Value.Lines[1].PercentOfRemainder);
    }

    [Fact]
    public void Agency_over_100_fails()
    {
        var intent = SplitIntentFactory.Create(
            Guid.NewGuid(),
            0,
            [],
            0,
            new AgencySlice(Guid.NewGuid(), 80, Guid.NewGuid(), 30));
        Assert.True(intent.IsFailure);
        Assert.Equal(ChargingErrorCodes.InvalidCut, intent.Errors.First().Code);
    }

    private static ChargeAggregate OpenSample()
    {
        var orange = Guid.NewGuid();
        var intent = SplitIntentFactory.Create(
            orange,
            10,
            [],
            null,
            new AgencySlice(Guid.NewGuid(), 70, Guid.NewGuid(), 0)).Value!;
        return ChargeAggregate.Open(Guid.NewGuid(), Guid.NewGuid(), 100, "BRL", Guid.NewGuid(), orange, intent).Value!;
    }
}
