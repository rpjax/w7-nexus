using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class MandateDomainTests
{
    [Fact]
    public void Admin_can_grant_any_preset()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var mandate = MemberMandate.Empty(member);

        var result = mandate.GrantPreset(PresetIds.Accountant, admin, grantorIsAdministrator: true, grantorMandate: null);

        Assert.True(result.IsSuccess);
        Assert.Contains(PresetIds.Accountant, mandate.AppliedPresets);
    }

    [Fact]
    public void Non_admin_cannot_grant_outside_umbrella()
    {
        var member = MemberId.New();
        var grantorId = MemberId.New();
        var grantor = MemberMandate.Empty(grantorId);
        grantor.GrantPreset(PresetIds.Recruiter, grantorId, grantorIsAdministrator: true, null);

        var mandate = MemberMandate.Empty(member);
        var result = mandate.GrantPreset(PresetIds.Accountant, grantorId, grantorIsAdministrator: false, grantor);

        Assert.True(result.IsFailure);
        Assert.Equal(MandateErrorCodes.AttenuationViolated, result.Errors.First().Code);
    }

    [Fact]
    public void Cascade_prune_removes_uncovered_grants()
    {
        var admin = MemberId.New();
        var mid = MemberId.New();
        var leaf = MemberId.New();

        var midMandate = MemberMandate.Empty(mid);
        midMandate.GrantPreset(PresetIds.OperationsManager, admin, true, null);

        var leafMandate = MemberMandate.Empty(leaf);
        leafMandate.GrantPreset(PresetIds.OperationsManager, mid, false, midMandate);

        midMandate.RevokePreset(PresetIds.OperationsManager);
        var pruned = leafMandate.PruneToUmbrella(midMandate, grantorIsAdministrator: false);

        Assert.True(pruned > 0);
        Assert.DoesNotContain(PresetIds.OperationsManager, leafMandate.AppliedPresets);
        Assert.Empty(leafMandate.Grants);
    }

    [Fact]
    public void Operation_specific_scope_works_and_covers_subset()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var created = MandateScope.OperationSpecific([a, b]);
        Assert.True(created.IsSuccess);

        var parent = created.Value!;
        var child = MandateScope.OperationSpecific([a]).Value!;
        Assert.True(parent.Covers(child));
        Assert.True(MandateScope.OperationAll().Covers(child));
    }

    [Fact]
    public void Deal_rejects_sum_over_100()
    {
        var created = AgencyDealAggregate.Open(MemberId.New(), MemberId.New(), 60, 50);
        Assert.True(created.IsFailure);
        Assert.Equal(MandateErrorCodes.DealPercentsInvalid, created.Errors.First().Code);
    }

    [Fact]
    public void Deal_accepts_sum_equal_100()
    {
        var created = AgencyDealAggregate.Open(MemberId.New(), MemberId.New(), 70, 30);
        Assert.True(created.IsSuccess);
    }

    [Fact]
    public void Deal_accepts_sum_under_100_residual()
    {
        var created = AgencyDealAggregate.Open(MemberId.New(), MemberId.New(), 40, 20);
        Assert.True(created.IsSuccess);
    }

    [Fact]
    public void Duplicate_preset_fails_without_event()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var mandate = MemberMandate.Empty(member);
        Assert.True(mandate.GrantPreset(PresetIds.Accountant, admin, true, null).IsSuccess);
        var count = mandate.UncommittedEvents.Count;
        var again = mandate.GrantPreset(PresetIds.Accountant, admin, true, null);
        Assert.True(again.IsFailure);
        Assert.Equal(MandateErrorCodes.PresetAlreadyGranted, again.Errors.First().Code);
        Assert.Equal(count, mandate.UncommittedEvents.Count);
    }

    [Fact]
    public void Unknown_capability_fails()
    {
        var mandate = MemberMandate.Empty(MemberId.New());
        var result = mandate.GrantCapability("nao_existe", MandateScope.Organization(), MemberId.New(), true, null);
        Assert.True(result.IsFailure);
        Assert.Equal(MandateErrorCodes.CapabilityUnknown, result.Errors.First().Code);
    }

    [Fact]
    public void Closed_deal_rejects_rate_change()
    {
        var deal = AgencyDealAggregate.Open(MemberId.New(), MemberId.New(), 70, 20).Value!;
        Assert.True(deal.Close().IsSuccess);
        var count = deal.UncommittedEvents.Count;
        var updated = deal.UpdatePercents(50, 20, deal.RecruiterId);
        Assert.True(updated.IsFailure);
        Assert.Equal(MandateErrorCodes.DealAlreadyClosed, updated.Errors.First().Code);
        Assert.Equal(count, deal.UncommittedEvents.Count);
    }

    [Fact]
    public void Stake_remove_replays_as_removed()
    {
        var stake = ShareholderStakeAggregate.Open(MemberId.New(), 10).Value!;
        Assert.True(stake.Remove().IsSuccess);
        var replayed = EventFold.Replay<ShareholderStakeAggregate>(stake.UncommittedEvents);
        Assert.True(replayed.IsRemoved);
        Assert.Equal(stake.AccountId, replayed.AccountId);
    }

    [Fact]
    public void Revoke_preset_replays_without_grants()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var mandate = MemberMandate.Empty(member);
        Assert.True(mandate.GrantPreset(PresetIds.Accountant, admin, true, null).IsSuccess);
        Assert.True(mandate.RevokePreset(PresetIds.Accountant).IsSuccess);
        var replayed = EventFold.Replay<MemberMandate>(mandate.UncommittedEvents);
        Assert.DoesNotContain(PresetIds.Accountant, replayed.AppliedPresets);
        Assert.Empty(replayed.Grants);
    }
}
