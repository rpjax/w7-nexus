using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RecordMemberAttrition;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.Events;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class MandateAttritionTests
{
    [Fact]
    public void Burned_member_has_no_live_capabilities()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var mandate = MemberMandate.Empty(member);
        Assert.True(mandate.GrantPreset(PresetIds.Recruiter, admin, true, null).IsSuccess);
        Assert.True(mandate.RecordAttrition("burned", "apreensao").IsSuccess);

        Assert.False(mandate.IsActive);
        Assert.False(mandate.HasCapability(Capabilities.Recrutar, MandateScope.CarteiraDirect()));
        Assert.Empty(mandate.AppliedPresets);
        Assert.Empty(mandate.Grants);
        Assert.Contains(mandate.UncommittedEvents, e => e is MandateGrantsPruned);
    }

    [Fact]
    public void Burned_rejects_saida_voluntaria()
    {
        var mandate = MemberMandate.Empty(MemberId.New());
        var result = mandate.RecordAttrition("burned", "saida_voluntaria");
        Assert.True(result.IsFailure);
        Assert.Equal(MandateErrorCodes.AttritionInvalid, result.Errors.First().Code);
    }

    [Fact]
    public void Burned_rejects_further_preset_grants()
    {
        var member = MemberId.New();
        var admin = MemberId.New();
        var mandate = MemberMandate.Empty(member);
        Assert.True(mandate.GrantPreset(PresetIds.Recruiter, admin, true, null).IsSuccess);
        Assert.True(mandate.RecordAttrition("burned", "apreensao").IsSuccess);
        var again = mandate.GrantPreset(PresetIds.Orange, admin, true, null);
        Assert.True(again.IsFailure);
        Assert.Equal(MandateErrorCodes.AttritionInvalid, again.Errors.First().Code);
    }

    [Fact]
    public void DropGrantsIssuedBy_replays_without_those_grants()
    {
        var admin = MemberId.New();
        var mid = MemberId.New();
        var leaf = MemberId.New();

        var midMandate = MemberMandate.Empty(mid);
        midMandate.GrantPreset(PresetIds.Recruiter, admin, true, null);

        var leafMandate = MemberMandate.Empty(leaf);
        leafMandate.GrantPreset(PresetIds.Recruiter, mid, false, midMandate);

        Assert.True(leafMandate.DropGrantsIssuedBy(mid) > 0);
        var replayed = EventFold.Replay<MemberMandate>(leafMandate.UncommittedEvents);
        Assert.Empty(replayed.Grants);
        Assert.Empty(replayed.AppliedPresets);
        Assert.Contains(leafMandate.UncommittedEvents, e => e is MandateGrantsPruned);
    }

    [Fact]
    public void Reparent_moves_granted_by_to_concedente()
    {
        var admin = MemberId.New();
        var mid = MemberId.New();
        var leaf = MemberId.New();

        var midMandate = MemberMandate.Empty(mid);
        midMandate.GrantPreset(PresetIds.Recruiter, admin, true, null);

        var leafMandate = MemberMandate.Empty(leaf);
        leafMandate.GrantPreset(PresetIds.Recruiter, mid, false, midMandate);

        Assert.True(leafMandate.ReparentGrantsIssuedBy(mid, admin) > 0);
        Assert.All(leafMandate.Grants, g => Assert.Equal(admin, g.GrantedBy));

        var replayed = EventFold.Replay<MemberMandate>(leafMandate.UncommittedEvents);
        Assert.All(replayed.Grants, g => Assert.Equal(admin, g.GrantedBy));
        Assert.Contains(PresetIds.Recruiter, replayed.AppliedPresets);
    }

    [Fact]
    public async Task Betrayed_drops_issued_grants_and_dead_downline_tree()
    {
        var (handler, store, directory, admin, mid, leaf) = SeedLineage();
        directory.Add(admin, isAdministrator: true);
        directory.Add(mid);
        directory.Add(leaf);

        var result = await handler.HandleAsync(new RecordMemberAttritionCommand(
            mid.ToString(), "betrayed", "traicao"));

        Assert.True(result.IsSuccess);
        var midMandate = await store.GetByMemberIdAsync(mid);
        Assert.Equal("betrayed", midMandate!.AttritionStatus);
        Assert.Empty(midMandate.AppliedPresets);
        Assert.False(midMandate.HasCapability(Capabilities.Recrutar, MandateScope.CarteiraDirect()));

        var leafMandate = await store.GetByMemberIdAsync(leaf);
        Assert.Empty(leafMandate!.Grants);
        Assert.False(leafMandate.HasCapability(Capabilities.Recrutar, MandateScope.CarteiraDirect()));
    }

    [Fact]
    public async Task Burned_drops_issued_grants_like_betrayed()
    {
        var (handler, store, directory, _, mid, leaf) = SeedLineage();
        directory.Add(mid);
        directory.Add(leaf);

        var result = await handler.HandleAsync(new RecordMemberAttritionCommand(
            mid.ToString(), "burned", "apreensao"));

        Assert.True(result.IsSuccess);
        var midMandate = await store.GetByMemberIdAsync(mid);
        Assert.Empty(midMandate!.AppliedPresets);
        var leafMandate = await store.GetByMemberIdAsync(leaf);
        Assert.Empty(leafMandate!.Grants);
        Assert.False(leafMandate.HasCapability(Capabilities.Recrutar, MandateScope.CarteiraDirect()));
    }

    [Fact]
    public async Task Burned_does_not_touch_agency_deal()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var admin = MemberId.New();
        var recruiter = MemberId.New();
        var op = MemberId.New();
        directory.Add(admin, isAdministrator: true);
        directory.Add(recruiter);
        directory.Add(op);

        var recruiterMandate = MemberMandate.Empty(recruiter);
        recruiterMandate.GrantPreset(PresetIds.Recruiter, admin, true, null);
        await store.SaveAsync(recruiterMandate);

        var deal = AgencyDealAggregate.Open(recruiter, op, 80, 10).Value!;
        await store.SaveAsync(deal);

        var handler = CreateHandler(store, directory, admin);
        Assert.True((await handler.HandleAsync(new RecordMemberAttritionCommand(
            recruiter.ToString(), "burned", "apreensao"))).IsSuccess);

        var still = await store.GetByIdAsync(deal.Id);
        Assert.True(still!.IsActive);
        Assert.Equal(recruiter, still.RecruiterId);
    }

    [Fact]
    public async Task Voluntary_exit_reparents_grants_and_carteira_to_concedente()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var admin = MemberId.New();
        var mid = MemberId.New();
        var leaf = MemberId.New();
        var op = MemberId.New();
        directory.Add(admin, isAdministrator: true);
        directory.Add(mid);
        directory.Add(leaf);
        directory.Add(op);

        var midMandate = MemberMandate.Empty(mid);
        midMandate.GrantPreset(PresetIds.Recruiter, admin, true, null);
        await store.SaveAsync(midMandate);

        var leafMandate = MemberMandate.Empty(leaf);
        leafMandate.GrantPreset(PresetIds.Recruiter, mid, false, midMandate);
        await store.SaveAsync(leafMandate);

        var deal = AgencyDealAggregate.Open(mid, op, 80, 10).Value!;
        await store.SaveAsync(deal);

        var handler = CreateHandler(store, directory, admin);
        var result = await handler.HandleAsync(new RecordMemberAttritionCommand(
            mid.ToString(), "left", "saida_voluntaria"));

        Assert.True(result.IsSuccess);
        var reparented = await store.GetByMemberIdAsync(leaf);
        Assert.NotEmpty(reparented!.Grants);
        Assert.All(reparented.Grants, g => Assert.Equal(admin, g.GrantedBy));
        Assert.True(reparented.HasCapability(Capabilities.Recrutar, MandateScope.CarteiraDirect()));

        var movedDeal = await store.GetByIdAsync(deal.Id);
        Assert.True(movedDeal!.IsActive);
        Assert.Equal(admin, movedDeal.RecruiterId);
        Assert.Equal(10, movedDeal.RecruiterPercent);
    }

    private static (
        RecordMemberAttritionHandler Handler,
        InMemoryMandateStore Store,
        InMemoryAccountDirectory Directory,
        MemberId Admin,
        MemberId Mid,
        MemberId Leaf) SeedLineage()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var admin = MemberId.New();
        var mid = MemberId.New();
        var leaf = MemberId.New();

        var midMandate = MemberMandate.Empty(mid);
        midMandate.GrantPreset(PresetIds.Recruiter, admin, true, null);
        store.SaveAsync(midMandate).GetAwaiter().GetResult();

        var grandchild = MemberMandate.Empty(leaf);
        grandchild.GrantPreset(PresetIds.Recruiter, mid, false, midMandate);
        store.SaveAsync(grandchild).GetAwaiter().GetResult();

        return (CreateHandler(store, directory, admin), store, directory, admin, mid, leaf);
    }

    private static RecordMemberAttritionHandler CreateHandler(
        InMemoryMandateStore store,
        InMemoryAccountDirectory directory,
        MemberId admin) =>
        new(
            new AdminRequestContext(admin.Value),
            new AllowAllMandateAccessPolicy(),
            directory,
            store,
            store,
            store,
            store);
}
