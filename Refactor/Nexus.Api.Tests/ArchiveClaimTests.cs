using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using HopAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop.Hop;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class ArchiveClaimTests
{
    [Fact]
    public async Task Archive_removes_claim_from_active_sum_and_debits_cash()
    {
        var fx = await Fx.CreateAsync();
        var target = fx.Claims[0];
        var handler = new ArchiveClaimHandler(
            new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimsStore, fx.Commit, new RecordingJournalWriter());

        var result = await handler.HandleAsync(new ArchiveClaimCommand(target.Id.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(ClaimStatus.Archived), result.Value!.Status);
        var reloaded = await fx.ClaimsStore.GetByIdAsync(target.Id);
        Assert.Equal(ClaimStatus.Archived, reloaded!.Status);
        var account = await fx.Accounts.GetByIdAsync(fx.Account.Id);
        var activeSum = (await fx.ClaimsStore.ListAsync(null, fx.Account.Id, null))
            .Where(c => c.IsActive)
            .Sum(c => c.Amount);
        Assert.Equal(80, activeSum);
        Assert.Equal(80, account!.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Archive_is_idempotent()
    {
        var fx = await Fx.CreateAsync();
        var handler = new ArchiveClaimHandler(
            new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimsStore, fx.Commit, new RecordingJournalWriter());
        var first = await handler.HandleAsync(new ArchiveClaimCommand(fx.Claims[0].Id.ToString()));
        var second = await handler.HandleAsync(new ArchiveClaimCommand(fx.Claims[0].Id.ToString()));
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var account = await fx.Accounts.GetByIdAsync(fx.Account.Id);
        Assert.Equal(80, account!.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Archive_when_cash_already_matches_without_claim_does_not_debit()
    {
        var fx = await Fx.CreateAsync();
        var target = fx.Claims[0];
        var account = await fx.Accounts.GetByIdAsync(fx.Account.Id);
        Assert.True(account!.Debit("BRL", target.Amount, "already-out").IsSuccess);
        await fx.Accounts.SaveAsync(account);
        var handler = new ArchiveClaimHandler(
            new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimsStore, fx.Commit, new RecordingJournalWriter());

        var result = await handler.HandleAsync(new ArchiveClaimCommand(target.Id.ToString()));

        Assert.True(result.IsSuccess);
        var after = await fx.Accounts.GetByIdAsync(fx.Account.Id);
        Assert.Equal(80, after!.BalanceOf("BRL"));
        Assert.Equal(ClaimStatus.Archived, (await fx.ClaimsStore.GetByIdAsync(target.Id))!.Status);
    }

    [Fact]
    public async Task Archive_rejects_when_cash_does_not_match()
    {
        var fx = await Fx.CreateAsync();
        var account = await fx.Accounts.GetByIdAsync(fx.Account.Id);
        Assert.True(account!.Credit("BRL", 3, "noise").IsSuccess);
        await fx.Accounts.SaveAsync(account);
        var handler = new ArchiveClaimHandler(
            new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimsStore, fx.Commit, new RecordingJournalWriter());

        var result = await handler.HandleAsync(new ArchiveClaimCommand(fx.Claims[0].Id.ToString()));

        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.UseReconcileEndpoint, result.Errors.First().Code);
    }

    private sealed class Fx
    {
        public WorldAccountAggregate Account { get; }
        public List<ClaimAggregate> Claims { get; }
        public InMemoryAccounts Accounts { get; } = new();
        public InMemoryClaims ClaimsStore { get; } = new();
        public LedgerCommit Commit { get; }

        private Fx(WorldAccountAggregate account, List<ClaimAggregate> claims)
        {
            Account = account;
            Claims = claims;
            Commit = new LedgerCommit(Accounts, ClaimsStore);
        }

        public static async Task<Fx> CreateAsync()
        {
            var account = WorldAccountAggregate.Open(WorldAccountKind.Bank, "bank", null, null, null, null).Value!;
            Assert.True(account.Credit("BRL", 100, "seed").IsSuccess);
            var chargeId = Guid.NewGuid();
            var claims = new List<ClaimAggregate>
            {
                ClaimAggregate.Open(Guid.NewGuid(), 20, "BRL", chargeId, account.Id, "A").Value!,
                ClaimAggregate.Open(Guid.NewGuid(), 80, "BRL", chargeId, account.Id, "B").Value!
            };
            var fx = new Fx(account, claims);
            await fx.Accounts.SaveAsync(account);
            foreach (var claim in claims)
                fx.ClaimsStore.Save(claim);
            return fx;
        }
    }

    private sealed class AllowAll : ILedgerAccess
    {
        public Task<bool> CanMaterializeAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsEligibleOrangeAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<Guid>> ListCarteiraOperatorIdsAsync(
            Guid recruiterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class AdminContext : IRequestContext
    {
        private readonly Guid _id;
        public AdminContext(Guid id) => _id = id;

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(
                new RequesterContext(_id.ToString(), [Roles.Administrator], [])));
    }

    private sealed class InMemoryAccounts : IWorldAccountRepository
    {
        private readonly EventStreamBag _streams = new();

        public Task<WorldAccountAggregate?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_streams.Load<WorldAccountAggregate>(accountId));

        public Task SaveAsync(WorldAccountAggregate account, CancellationToken cancellationToken = default)
        {
            _streams.Append(account.Id, account.UncommittedEvents);
            account.ClearUncommitted();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorldAccountAggregate>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorldAccountAggregate>>([]);

        public Task<IReadOnlyList<WorldAccountTransaction>> ListTransactionsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorldAccountTransaction>>([]);
    }

    private sealed class InMemoryClaims : IClaimRepository
    {
        private readonly EventStreamBag _streams = new();

        public Task<ClaimAggregate?> GetByIdAsync(Guid claimId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_streams.Load<ClaimAggregate>(claimId));

        public Task<IReadOnlyList<ClaimAggregate>> ListAsync(
            Guid? originChargeId,
            Guid? locationAccountId,
            Guid? beneficiaryId,
            CancellationToken cancellationToken = default)
        {
            var items = _streams.StreamIds.Select(id => _streams.Load<ClaimAggregate>(id)!).AsEnumerable();
            if (originChargeId is not null)
                items = items.Where(c => c.OriginChargeId == originChargeId);
            if (locationAccountId is not null)
                items = items.Where(c => c.LocationAccountId == locationAccountId);
            if (beneficiaryId is not null)
                items = items.Where(c => c.BeneficiaryId == beneficiaryId);
            return Task.FromResult<IReadOnlyList<ClaimAggregate>>(items.ToList());
        }

        public void Save(ClaimAggregate claim)
        {
            _streams.Append(claim.Id, claim.UncommittedEvents);
            claim.ClearUncommitted();
        }
    }

    private sealed class LedgerCommit : ILedgerCommit
    {
        private readonly InMemoryAccounts _accounts;
        private readonly InMemoryClaims _claims;

        public LedgerCommit(InMemoryAccounts accounts, InMemoryClaims claims)
        {
            _accounts = accounts;
            _claims = claims;
        }

        public async Task SaveAsync(
            IReadOnlyList<WorldAccountAggregate> accounts,
            IReadOnlyList<ClaimAggregate> claims,
            HopAggregate? hop = null,
            ChargeAggregate? charge = null,
            CancellationToken cancellationToken = default)
        {
            foreach (var account in accounts)
                await _accounts.SaveAsync(account, cancellationToken);
            foreach (var claim in claims)
                _claims.Save(claim);
        }
    }
}
