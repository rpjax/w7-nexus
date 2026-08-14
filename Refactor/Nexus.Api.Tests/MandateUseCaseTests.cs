using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Authorization.Application.Models;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.CloseAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantPreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertShareholderStake;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class MandateUseCaseTests
{
    [Fact]
    public async Task Operator_preset_requires_active_deal()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var operatorId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(operatorId);

        var handler = new GrantPresetHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store,
            store);

        var result = await handler.HandleAsync(new GrantPresetCommand(operatorId.ToString(), PresetIds.Operator));

        Assert.True(result.IsFailure);
        Assert.Equal(MandateErrorCodes.OperatorRequiresDeal, result.Errors.First().Code);
        Assert.Contains("Deals", result.Errors.First().Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_deal_requires_admin_recruiter_when_pct_zero()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var recruiterId = MemberId.New();
        var operatorId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(recruiterId);
        directory.Add(operatorId);

        var handler = new UpsertAgencyDealHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store);

        var result = await handler.HandleAsync(new UpsertAgencyDealCommand(
            recruiterId.ToString(),
            operatorId.ToString(),
            80,
            0));

        Assert.True(result.IsFailure);
        Assert.Equal(MandateErrorCodes.DealRootRequiresAdmin, result.Errors.First().Code);
    }

    [Fact]
    public async Task Root_deal_with_admin_recruiter_succeeds()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var operatorId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(operatorId);

        var handler = new UpsertAgencyDealHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store);

        var result = await handler.HandleAsync(new UpsertAgencyDealCommand(
            adminId.ToString(),
            operatorId.ToString(),
            80,
            0));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Shareholder_sum_over_100_fails_on_upsert()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var a = MemberId.New();
        var b = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(a);
        directory.Add(b);

        var handler = new UpsertShareholderStakeHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store);

        Assert.True((await handler.HandleAsync(new UpsertShareholderStakeCommand(a.ToString(), 60))).IsSuccess);
        var second = await handler.HandleAsync(new UpsertShareholderStakeCommand(b.ToString(), 50));

        Assert.True(second.IsFailure);
        Assert.Equal(MandateErrorCodes.StakeTotalExceedsHundred, second.Errors.First().Code);
    }

    [Fact]
    public async Task Operator_preset_succeeds_after_root_deal()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var operatorId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(operatorId);

        var deals = new UpsertAgencyDealHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store);
        Assert.True((await deals.HandleAsync(new UpsertAgencyDealCommand(
            adminId.ToString(), operatorId.ToString(), 80, 0))).IsSuccess);

        var grant = new GrantPresetHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store,
            store);
        var result = await grant.HandleAsync(new GrantPresetCommand(operatorId.ToString(), PresetIds.Operator));
        Assert.True(result.IsSuccess);

        var mandate = await store.GetByMemberIdAsync(operatorId);
        Assert.Contains(PresetIds.Operator, mandate!.AppliedPresets);
        Assert.Empty(mandate.UncommittedEvents);
    }

    [Fact]
    public async Task Close_deal_blocked_while_operator_preset_exists()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var operatorId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(operatorId);

        await new UpsertAgencyDealHandler(
            new FakeRequestContext(adminId), new AlwaysAdminPolicy(), directory, store, store)
            .HandleAsync(new UpsertAgencyDealCommand(adminId.ToString(), operatorId.ToString(), 80, 0));
        await new GrantPresetHandler(
            new FakeRequestContext(adminId), new AlwaysAdminPolicy(), directory, store, store, store)
            .HandleAsync(new GrantPresetCommand(operatorId.ToString(), PresetIds.Operator));

        var close = await new CloseAgencyDealHandler(
            new FakeRequestContext(adminId), new AlwaysAdminPolicy(), store, store)
            .HandleAsync(new CloseAgencyDealCommand(operatorId.ToString()));

        Assert.True(close.IsFailure);
        Assert.Equal(MandateErrorCodes.DealCannotCloseWhileOperatorPreset, close.Errors.First().Code);
        Assert.True(await store.HasActiveDealForOperatorAsync(operatorId));
    }

    [Fact]
    public async Task Nested_grant_succeeds_with_conceder_mandato_without_admin_role()
    {
        var store = new InMemoryMandateStore();
        var directory = new InMemoryAccountDirectory();
        var adminId = MemberId.New();
        var recruiterId = MemberId.New();
        var leafId = MemberId.New();
        directory.Add(adminId, isAdministrator: true);
        directory.Add(recruiterId);
        directory.Add(leafId);

        Assert.True((await new GrantPresetHandler(
            new FakeRequestContext(adminId),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store,
            store).HandleAsync(new GrantPresetCommand(recruiterId.ToString(), PresetIds.Recruiter))).IsSuccess);

        var nested = await new GrantPresetHandler(
            new FakeRequestContext(recruiterId, admin: false),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store,
            store).HandleAsync(new GrantPresetCommand(leafId.ToString(), PresetIds.Recruiter));
        Assert.True(nested.IsSuccess);

        var stranger = MemberId.New();
        directory.Add(stranger);
        var denied = await new GrantPresetHandler(
            new FakeRequestContext(stranger, admin: false),
            new AlwaysAdminPolicy(),
            directory,
            store,
            store,
            store).HandleAsync(new GrantPresetCommand(leafId.ToString(), PresetIds.Operator));
        Assert.False(denied.IsAuthorized);
    }
}

file sealed class AlwaysAdminPolicy : IMandateAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeAdministratorAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());
}

file sealed class FakeRequestContext : IRequestContext
{
    private readonly MemberId _accountId;
    private readonly bool _admin;

    public FakeRequestContext(MemberId accountId, bool admin = true)
    {
        _accountId = accountId;
        _admin = admin;
    }

    public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        IResult<RequesterContext> result = Result<RequesterContext>.Success(
            new RequesterContext(_accountId.ToString(), _admin ? [Roles.Administrator] : [], []));
        return Task.FromResult(result);
    }
}
