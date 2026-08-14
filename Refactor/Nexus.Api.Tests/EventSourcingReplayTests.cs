using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Events;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Events;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Events;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;
using WorldAccount = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class EventSourcingReplayTests
{
    [Fact]
    public void Account_replay_matches_live_state()
    {
        var live = Account.Create("alice", "hash", [Roles.Administrator]);
        Assert.True(live.ChangeUsername("alice2").IsSuccess);
        Assert.True(live.Disable().IsSuccess);

        var replayed = EventFold.Replay<Account>(live.UncommittedEvents);
        Assert.Equal(live.Id, replayed.Id);
        Assert.Equal(live.Username, replayed.Username);
        Assert.Equal(live.Status, replayed.Status);
        Assert.Equal(live.Roles, replayed.Roles);
    }

    [Fact]
    public void Operation_replay_keeps_assignments_until_close()
    {
        var live = OperationAggregate.Create("Front").Value!;
        Assert.True(live.TransitionTo(OperationStatus.Active).IsSuccess);
        var member = Guid.NewGuid();
        Assert.True(live.AssignOperator(member).IsSuccess);
        Assert.True(live.TransitionTo(OperationStatus.Closed).IsSuccess);

        var replayed = EventFold.Replay<OperationAggregate>(live.UncommittedEvents);
        Assert.Equal(live.Id, replayed.Id);
        Assert.Equal(live.Status, replayed.Status);
        Assert.Empty(replayed.AssignedOperatorIds);
        Assert.Contains(live.UncommittedEvents, e => e is OperationAssignmentsCleared);
    }

    [Fact]
    public void Mandate_and_deal_replay()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var live = MemberMandate.Empty(member);
        Assert.True(live.GrantPreset(PresetIds.Accountant, admin, grantorIsAdministrator: true, grantorMandate: null).IsSuccess);

        var replayed = EventFold.Replay<MemberMandate>(live.UncommittedEvents);
        Assert.Equal(live.MemberId, replayed.MemberId);
        Assert.Equal(live.AppliedPresets.OrderBy(x => x), replayed.AppliedPresets.OrderBy(x => x));

        var deal = AgencyDeal.Open(admin, member, 80, 0).Value!;
        Assert.True(deal.Close().IsSuccess);
        var replayedDeal = EventFold.Replay<AgencyDeal>(deal.UncommittedEvents);
        Assert.Equal(AgencyDealStatus.Closed, replayedDeal.Status);
        Assert.Equal(deal.Id, replayedDeal.Id);
    }

    [Fact]
    public void Charge_paid_replays_to_paid()
    {
        var orange = Guid.NewGuid();
        var split = SplitIntentFactory.Create(
            orange,
            10,
            [],
            5,
            new AgencySlice(Guid.NewGuid(), 20, Guid.NewGuid(), 10)).Value!;
        var opened = ChargeAggregate.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            "BRL",
            Guid.NewGuid(),
            orange,
            split);
        Assert.True(opened.IsSuccess);
        var live = opened.Value!;
        Assert.True(live.AssignExternalReference("ext-1").IsSuccess);
        Assert.True(live.MarkPaid().IsSuccess);

        var replayed = EventFold.Replay<ChargeAggregate>(live.UncommittedEvents);
        Assert.Equal(ChargeStatus.Paid, replayed.Status);
        Assert.Equal(live.Id, replayed.Id);
        Assert.Equal("ext-1", replayed.ExternalReference);
        Assert.True(live.MarkMaterialized(9, "BRL", Guid.NewGuid()).IsSuccess);
        var materialized = EventFold.Replay<ChargeAggregate>(live.UncommittedEvents);
        Assert.Equal(ChargeStatus.Materialized, materialized.Status);
        Assert.Equal(9, materialized.NetAmount);
    }

    [Fact]
    public void Claim_open_replays()
    {
        var live = ClaimAggregate.Open(Guid.NewGuid(), 12, "BRL", Guid.NewGuid(), Guid.NewGuid(), "Orange").Value!;
        var replayed = EventFold.Replay<ClaimAggregate>(live.UncommittedEvents);
        Assert.Equal(live.Id, replayed.Id);
        Assert.Equal(12, replayed.Amount);
        Assert.Equal(12, replayed.BirthAmount);
        Assert.Equal(ClaimStatus.Active, replayed.Status);
    }

    [Fact]
    public void WorldAccount_observation_replays_per_currency()
    {
        var live = WorldAccount.Open(WorldAccountKind.Crypto, "c", null, null, "USD", 5).Value!;
        Assert.True(live.Credit("BRL", 7).IsSuccess);
        var replayed = EventFold.Replay<WorldAccount>(live.UncommittedEvents);
        Assert.Equal(7, replayed.BalanceOf("BRL"));
        Assert.Equal(0, replayed.BalanceOf("USD"));
        Assert.Equal(5, replayed.QuotaOf("USD"));
    }

    [Fact]
    public void Failed_mutation_does_not_change_replay()
    {
        var live = OperationAggregate.Create("Front").Value!;
        Assert.True(live.TransitionTo(OperationStatus.Closed).IsSuccess);
        var before = live.UncommittedEvents.ToArray();
        Assert.True(live.TransitionTo(OperationStatus.Active).IsFailure);
        Assert.Equal(before.Length, live.UncommittedEvents.Count);
        Assert.Equal(OperationStatus.Closed, EventFold.Replay<OperationAggregate>(live.UncommittedEvents).Status);
    }
}
