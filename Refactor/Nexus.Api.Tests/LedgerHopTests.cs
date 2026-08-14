using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Authenticated.Queries;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Events;
using Refactor.Nexus.Api.Ledger.Domain.Services;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using HopAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop.Hop;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class HopAllocatorTests
{
    [Fact]
    public void Cut_10_percent_scales_20_50_30_and_opens_path_cut()
    {
        var origin = Guid.NewGuid();
        var charge = Guid.NewGuid();
        var orange = Guid.NewGuid();
        var orangeAccount = Guid.NewGuid();
        var dest = Guid.NewGuid();
        var bundle = new[]
        {
            Item(20, origin, charge),
            Item(50, origin, charge),
            Item(30, origin, charge)
        };

        var plan = HopAllocator.Plan(
            bundle,
            [new HopDestSpec(dest, 90, "BRL")],
            new HopCutSpec(orange, 10, false, orangeAccount)).Value!;

        Assert.Equal(0, plan.LossAmount);
        Assert.Equal(18, plan.Adjustments.Single(a => a.Amount == 18).Amount);
        Assert.Equal(45, plan.Adjustments.Single(a => a.Amount == 45).Amount);
        Assert.Equal(27, plan.Adjustments.Single(a => a.Amount == 27).Amount);
        Assert.Equal(10, plan.NewClaims.Single(c => c.Kind == ClaimAggregate.PathCutKind).Amount);
    }

    private static HopBundleItem Item(decimal amount, Guid origin, Guid charge) =>
        new(Guid.NewGuid(), Guid.NewGuid(), amount, charge, origin, "Agency", "BRL", amount, "BRL");
}

public sealed class HopAndRepassTests
{
    [Fact]
    public async Task Hop_100_to_95_shrinks_and_balances_match()
    {
        var fx = await HopFx.CreateAsync();
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null,
            false,
            AttritionCause.ErroOperacional));
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.LossAmount);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var dest = await fx.Accounts.GetByIdAsync(fx.Dest.Id);
        var atDest = (await fx.ClaimStore.ListAsync(null, fx.Dest.Id, null)).Where(c => c.IsActive).ToList();
        var atOrigin = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).ToList();
        Assert.Equal(0, origin!.BalanceOf("BRL"));
        Assert.Equal(95, dest!.BalanceOf("BRL"));
        Assert.Equal(0, atOrigin.Sum(c => c.Amount));
        Assert.Equal(95, atDest.Sum(c => c.Amount));
        Assert.Equal(origin.BalanceOf("BRL"), atOrigin.Sum(c => c.Amount));
        Assert.Equal(dest.BalanceOf("BRL"), atDest.Sum(c => c.Amount));
        var hopFact = Assert.Single(fx.Journal.Facts.OfType<LedgerHopRegistered>());
        Assert.Equal(result.Value.HopId, hopFact.HopId);
        Assert.NotEqual(Guid.Empty, hopFact.ActedBy);
    }

    [Fact]
    public async Task Hop_loss_without_cause_is_rejected()
    {
        var fx = await HopFx.CreateAsync();
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null));
        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.CauseRequired, result.Errors.First().Code);
    }

    [Fact]
    public async Task Hop_keep_remainder_leaves_origin_balance()
    {
        var fx = await HopFx.CreateAsync();
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null,
            true,
            null));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.LossAmount);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var dest = await fx.Accounts.GetByIdAsync(fx.Dest.Id);
        var atDest = (await fx.ClaimStore.ListAsync(null, fx.Dest.Id, null)).Where(c => c.IsActive).ToList();
        var atOrigin = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).ToList();
        Assert.Equal(5, origin!.BalanceOf("BRL"));
        Assert.Equal(95, dest!.BalanceOf("BRL"));
        Assert.Equal(5, atOrigin.Sum(c => c.Amount));
        Assert.Equal(95, atDest.Sum(c => c.Amount));
    }

    [Fact]
    public async Task Redenomination_archives_origin_and_opens_declared_dest()
    {
        var fx = await HopFx.CreateAsync();
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 40, "USDT")],
            null));
        Assert.True(result.IsSuccess);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var dest = await fx.Accounts.GetByIdAsync(fx.Dest.Id);
        Assert.Equal(0, origin!.BalanceOf("BRL"));
        Assert.Equal(40, dest!.BalanceOf("USDT"));
        Assert.Equal(0, (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).Sum(c => c.Amount));
        var destClaims = (await fx.ClaimStore.ListAsync(null, fx.Dest.Id, null)).Where(c => c.IsActive).ToList();
        Assert.Equal(40, destClaims.Sum(c => c.Amount));
        Assert.All(destClaims, c => Assert.Equal("USDT", c.Currency));
    }

    [Fact]
    public async Task Repass_marks_repassed_debits_origin_and_does_not_credit_payout()
    {
        var fx = await HopFx.CreateAsync();
        var beforePayout = fx.Payout.BalanceOf("BRL");
        var result = await fx.Repass.HandleAsync(new RepassClaimsCommand(
            fx.Origin.Id.ToString(),
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            fx.Payout.Id.ToString()));
        Assert.True(result.IsSuccess);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var payout = await fx.Accounts.GetByIdAsync(fx.Payout.Id);
        Assert.Equal(0, origin!.BalanceOf("BRL"));
        Assert.Equal(beforePayout, payout!.BalanceOf("BRL"));
        var claims = await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null);
        Assert.All(claims, c => Assert.Equal(ClaimStatus.Repassed, c.Status));
        Assert.Equal(0, claims.Where(c => c.IsActive).Sum(c => c.Amount));
        var repassFact = Assert.Single(fx.Journal.Facts.OfType<LedgerClaimsRepassed>());
        Assert.Equal(fx.Origin.Id, repassFact.OriginAccountId);
        Assert.NotEqual(Guid.Empty, repassFact.ActedBy);
    }

    [Fact]
    public async Task Second_path_cut_same_orange_and_origin_fails()
    {
        var fx = await HopFx.CreateAsync();
        var orange = Guid.NewGuid();
        var first = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [],
            new HopCutInput(orange.ToString(), 10, true, null)));
        Assert.True(first.IsSuccess);

        var remaining = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive && c.Kind != ClaimAggregate.PathCutKind).ToList();
        var second = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            remaining.Select(c => c.Id.ToString()).ToList(),
            [],
            new HopCutInput(orange.ToString(), 10, true, null)));
        Assert.True(second.IsFailure);
        Assert.Equal(LedgerErrorCodes.CutAlreadyTaken, second.Errors.First().Code);
    }

    [Fact]
    public async Task Emission_orange_cannot_take_path_cut()
    {
        var fx = await HopFx.CreateAsync();
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [],
            new HopCutInput(fx.Charge.OrangeMemberId.ToString(), 10, true, null)));
        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.CutAlreadyTaken, result.Errors.First().Code);
    }

    [Fact]
    public async Task In_place_cut_does_not_change_account_balances()
    {
        var fx = await HopFx.CreateAsync();
        var before = fx.Origin.BalanceOf("BRL");
        var result = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [],
            new HopCutInput(Guid.NewGuid().ToString(), 10, true, null)));
        Assert.True(result.IsSuccess);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var claims = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).ToList();
        Assert.Equal(before, origin!.BalanceOf("BRL"));
        Assert.Equal(origin.BalanceOf("BRL"), claims.Sum(c => c.Amount));
        Assert.Equal(10, claims.Single(c => c.Kind == ClaimAggregate.PathCutKind).Amount);
    }

    [Fact]
    public async Task Statement_keeps_birth_estimate_after_hop_loss()
    {
        var fx = await HopFx.CreateAsync();
        var slice = fx.Claims[0];
        Assert.True((await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null,
            false,
            AttritionCause.ErroOperacional))).IsSuccess);

        var live = await fx.ClaimStore.GetByIdAsync(slice.Id);
        Assert.Equal(20, live!.BirthAmount);
        Assert.Equal(19, live.Amount);
        Assert.Equal(fx.Dest.Id, live.LocationAccountId);

        var statement = await new GetMyStatementHandler(
            new AdminContext(slice.BeneficiaryId),
            fx.ClaimStore,
            new AllowAll(),
            fx.Journal).HandleAsync(new GetMyStatementQuery());
        Assert.True(statement.IsSuccess);
        var line = Assert.Single(statement.Value!.Items);
        Assert.Equal("estimate", line.Phase);
        Assert.Equal(20, line.EstimateAmount);
        Assert.Null(line.ReleasedAmount);
        Assert.Null(typeof(StatementLine).GetProperty("LocationAccountId"));
        Assert.Null(typeof(StatementLine).GetProperty("Amount"));

        var accountant = await new ListClaimsHandler(
            new AdminContext(Guid.NewGuid()),
            new AllowAll(),
            fx.ClaimStore,
            fx.Journal).HandleAsync(new ListClaimsQuery(fx.Charge.Id.ToString(), null, slice.BeneficiaryId.ToString()));
        var view = Assert.Single(accountant.Value!.Items);
        Assert.Equal(19, view.Amount);
        Assert.Equal(fx.Dest.Id, view.LocationAccountId);
    }

    [Fact]
    public async Task Reveal_switches_statement_to_pending_snapshot()
    {
        var fx = await HopFx.CreateAsync();
        var slice = fx.Claims[0];
        Assert.True((await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null,
            false,
            AttritionCause.ErroOperacional))).IsSuccess);

        var first = await fx.Reveal.HandleAsync(new RevealClaimCommand(slice.Id.ToString(), "Ajuste de rota."));
        Assert.True(first.IsSuccess);
        var again = await fx.Reveal.HandleAsync(new RevealClaimCommand(slice.Id.ToString(), "Ajuste de rota."));
        Assert.True(again.IsSuccess);
        var other = await fx.Reveal.HandleAsync(new RevealClaimCommand(slice.Id.ToString(), "Outra causa."));
        Assert.True(other.IsFailure);
        Assert.Equal(LedgerErrorCodes.AlreadyRevealed, other.Errors.First().Code);

        var statement = await new GetMyStatementHandler(
            new AdminContext(slice.BeneficiaryId),
            fx.ClaimStore,
            new AllowAll(),
            fx.Journal).HandleAsync(new GetMyStatementQuery());
        var line = Assert.Single(statement.Value!.Items);
        Assert.Equal("pending", line.Phase);
        Assert.Equal(20, line.EstimateAmount);
        Assert.Equal(19, line.ReleasedAmount);
        Assert.Equal("BRL", line.ReleasedCurrency);
        Assert.Equal("Ajuste de rota.", line.Summary);
        Assert.DoesNotContain("world-account", line.Summary ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fx.Journal.Facts, f => f is LedgerClaimRevealed);
        Assert.Contains(fx.Journal.Facts, f => f is LedgerStatementRead);
    }

    [Fact]
    public async Task Lost_writes_off_active_claims_and_zeros_balances()
    {
        var fx = await HopFx.CreateAsync();
        var lost = new MarkAccountLostHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var result = await lost.HandleAsync(new MarkAccountLostCommand(fx.Origin.Id.ToString(), "apreensao"));
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.WrittenOff);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        Assert.Equal(BalanceStatus.Lost, origin!.BalanceStatus);
        Assert.Equal(0, origin.BalanceOf("BRL"));
        var claims = await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null);
        Assert.All(claims, c => Assert.Equal(ClaimStatus.Lost, c.Status));
        Assert.Equal(0, claims.Where(c => c.IsActive).Sum(c => c.Amount));

        var again = await lost.HandleAsync(new MarkAccountLostCommand(fx.Origin.Id.ToString(), "apreensao"));
        Assert.True(again.IsSuccess);
        Assert.Equal(0, again.Value!.WrittenOff);
        Assert.Equal(2, fx.Journal.Facts.OfType<LedgerAccountMarkedLost>().Count());
    }

    [Fact]
    public async Task Frozen_keeps_claims_and_allows_hop()
    {
        var fx = await HopFx.CreateAsync();
        fx.Origin.SetBalanceStatus(BalanceStatus.Frozen);
        await fx.Accounts.SaveAsync(fx.Origin);
        var claims = await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null);
        Assert.All(claims, c => Assert.Equal(ClaimStatus.Active, c.Status));

        var hop = await fx.Hop.HandleAsync(new RegisterHopCommand(
            fx.Origin.Id.ToString(),
            "BRL",
            fx.Claims.Select(c => c.Id.ToString()).ToList(),
            [new HopDestinationInput(fx.Dest.Id.ToString(), 95, "BRL")],
            null,
            false,
            AttritionCause.ErroOperacional));
        Assert.True(hop.IsSuccess);
    }

    [Fact]
    public async Task Reconcile_shortage_100_to_90_scales_20_50_30()
    {
        var fx = await HopFx.CreateAsync();
        var handler = new ReconcileAccountHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var result = await handler.HandleAsync(new ReconcileAccountCommand(
            fx.Origin.Id.ToString(), "BRL", 90, "erro_operacional", null));
        Assert.True(result.IsSuccess);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var claims = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).OrderBy(c => c.Amount).ToList();
        Assert.Equal(90, origin!.BalanceOf("BRL"));
        Assert.Equal([18m, 27m, 45m], claims.Select(c => c.Amount).ToList());
        Assert.Equal(90, claims.Sum(c => c.Amount));
        Assert.Single(fx.Journal.Facts.OfType<LedgerAccountReconciled>());
    }

    [Fact]
    public async Task Reconcile_surplus_opens_residual_org_claim()
    {
        var fx = await HopFx.CreateAsync();
        var handler = new ReconcileAccountHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var result = await handler.HandleAsync(new ReconcileAccountCommand(
            fx.Origin.Id.ToString(), "BRL", 110, "desconhecido", null));
        Assert.True(result.IsSuccess);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var claims = (await fx.ClaimStore.ListAsync(null, fx.Origin.Id, null)).Where(c => c.IsActive).ToList();
        Assert.Equal(110, origin!.BalanceOf("BRL"));
        Assert.Equal(110, claims.Sum(c => c.Amount));
        var residual = Assert.Single(claims, c => c.Kind == SplitIntent.ResidualOrg);
        Assert.Equal(10, residual.Amount);
        Assert.Equal(OrganizationParty.Id, residual.BeneficiaryId);
    }

    [Fact]
    public async Task Reverse_charge_marks_claims_reversed_and_debits_accounts()
    {
        var fx = await HopFx.CreateAsync();
        var handler = new ReverseChargeHandler(
            new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Charges, fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var result = await handler.HandleAsync(new ReverseChargeCommand(fx.Charge.Id.ToString(), AttritionCause.Estorno));
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.ReversedClaims);

        var origin = await fx.Accounts.GetByIdAsync(fx.Origin.Id);
        var charge = await fx.Charges.GetByIdAsync(fx.Charge.Id);
        var claims = await fx.ClaimStore.ListAsync(fx.Charge.Id, null, null);
        Assert.Equal(0, origin!.BalanceOf("BRL"));
        Assert.Equal(ChargeStatus.Reversed, charge!.Status);
        Assert.All(claims, c => Assert.Equal(ClaimStatus.Reversed, c.Status));
        Assert.Single(fx.Journal.Facts.OfType<LedgerChargeReversed>());
    }

    [Fact]
    public async Task Missing_cause_is_rejected()
    {
        var fx = await HopFx.CreateAsync();
        var lost = new MarkAccountLostHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var result = await lost.HandleAsync(new MarkAccountLostCommand(fx.Origin.Id.ToString(), ""));
        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.CauseRequired, result.Errors.First().Code);

        var reconcile = new ReconcileAccountHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), fx.Accounts, fx.ClaimStore, fx.Commit, fx.Journal);
        var bad = await reconcile.HandleAsync(new ReconcileAccountCommand(
            fx.Origin.Id.ToString(), "BRL", 90, "not-a-cause", null));
        Assert.True(bad.IsFailure);
        Assert.Equal(LedgerErrorCodes.CauseRequired, bad.Errors.First().Code);
    }

    private sealed class HopFx
    {
        public ChargeAggregate Charge { get; }
        public WorldAccountAggregate Origin { get; }
        public WorldAccountAggregate Dest { get; }
        public WorldAccountAggregate Payout { get; }
        public List<ClaimAggregate> Claims { get; }
        public InMemoryCharges Charges { get; } = new();
        public InMemoryAccounts Accounts { get; } = new();
        public InMemoryClaims ClaimStore { get; } = new();
        public InMemoryHops Hops { get; } = new();
        public RecordingJournalWriter Journal { get; } = new();
        public RegisterHopHandler Hop { get; }
        public RepassClaimsHandler Repass { get; }
        public RevealClaimHandler Reveal { get; }
        public LedgerCommit Commit { get; }

        private HopFx(ChargeAggregate charge, WorldAccountAggregate origin, WorldAccountAggregate dest, WorldAccountAggregate payout, List<ClaimAggregate> claims)
        {
            Charge = charge;
            Origin = origin;
            Dest = dest;
            Payout = payout;
            Claims = claims;
            Commit = new LedgerCommit(Charges, Accounts, ClaimStore, Hops);
            Hop = new RegisterHopHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), Charges, Accounts, ClaimStore, Commit, Journal);
            Repass = new RepassClaimsHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), Accounts, ClaimStore, Commit, Journal);
            Reveal = new RevealClaimHandler(new AdminContext(Guid.NewGuid()), new AllowAll(), ClaimStore, Commit, Journal);
        }

        public static async Task<HopFx> CreateAsync()
        {
            var orange = Guid.NewGuid();
            var intent = SplitIntentFactory.Create(
                orange,
                10,
                [],
                0,
                new AgencySlice(Guid.NewGuid(), 80, Guid.NewGuid(), 0)).Value!;
            var charge = ChargeAggregate.Open(Guid.NewGuid(), Guid.NewGuid(), 100, "BRL", Guid.NewGuid(), orange, intent).Value!;
            charge.MarkPaid();
            var origin = WorldAccountAggregate.Open(WorldAccountKind.Bank, "origin", null, null, null, null).Value!;
            origin.Credit("BRL", 100, "seed");
            var dest = WorldAccountAggregate.Open(WorldAccountKind.Bank, "dest", null, null, null, null).Value!;
            var payout = WorldAccountAggregate.Open(WorldAccountKind.Payout, "payout", null, null, null, null).Value!;
            var claims = new List<ClaimAggregate>
            {
                ClaimAggregate.Open(Guid.NewGuid(), 20, "BRL", charge.Id, origin.Id, "A").Value!,
                ClaimAggregate.Open(Guid.NewGuid(), 50, "BRL", charge.Id, origin.Id, "B").Value!,
                ClaimAggregate.Open(Guid.NewGuid(), 30, "BRL", charge.Id, origin.Id, "C").Value!
            };

            var fx = new HopFx(charge, origin, dest, payout, claims);
            await fx.Charges.SaveAsync(charge);
            await fx.Accounts.SaveAsync(origin);
            await fx.Accounts.SaveAsync(dest);
            await fx.Accounts.SaveAsync(payout);
            foreach (var claim in claims)
                fx.ClaimStore.Save(claim);
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

    private sealed class InMemoryCharges : IChargeRepository
    {
        private readonly EventStreamBag _streams = new();

        public Task<ChargeAggregate?> GetByIdAsync(Guid chargeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_streams.Load<ChargeAggregate>(chargeId));

        public Task<ChargeAggregate?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default) =>
            Task.FromResult<ChargeAggregate?>(null);

        public Task SaveAsync(ChargeAggregate charge, CancellationToken cancellationToken = default)
        {
            _streams.Append(charge.Id, charge.UncommittedEvents);
            charge.ClearUncommitted();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChargeAggregate>> ListAsync(
            Guid? operationId,
            Guid? operatorMemberId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChargeAggregate>>([]);
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
            Task.FromResult<IReadOnlyList<WorldAccountAggregate>>(
                _streams.StreamIds.Select(id => _streams.Load<WorldAccountAggregate>(id)!).ToList());

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

    private sealed class InMemoryHops : IHopRepository
    {
        private readonly EventStreamBag _streams = new();

        public Task<HopAggregate?> GetByIdAsync(Guid hopId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_streams.Load<HopAggregate>(hopId));

        public Task<IReadOnlyList<HopAggregate>> ListAsync(Guid? originAccountId, CancellationToken cancellationToken = default)
        {
            var items = _streams.StreamIds.Select(id => _streams.Load<HopAggregate>(id)!).AsEnumerable();
            if (originAccountId is not null)
                items = items.Where(h => h.OriginAccountId == originAccountId);
            return Task.FromResult<IReadOnlyList<HopAggregate>>(items.ToList());
        }

        public void Save(HopAggregate hop)
        {
            _streams.Append(hop.Id, hop.UncommittedEvents);
            hop.ClearUncommitted();
        }
    }

    private sealed class LedgerCommit : ILedgerCommit
    {
        private readonly InMemoryCharges _charges;
        private readonly InMemoryAccounts _accounts;
        private readonly InMemoryClaims _claims;
        private readonly InMemoryHops _hops;

        public LedgerCommit(InMemoryCharges charges, InMemoryAccounts accounts, InMemoryClaims claims, InMemoryHops hops)
        {
            _charges = charges;
            _accounts = accounts;
            _claims = claims;
            _hops = hops;
        }

        public async Task SaveAsync(
            IReadOnlyList<WorldAccountAggregate> accounts,
            IReadOnlyList<ClaimAggregate> claims,
            HopAggregate? hop = null,
            ChargeAggregate? charge = null,
            CancellationToken cancellationToken = default)
        {
            if (charge is not null)
                await _charges.SaveAsync(charge, cancellationToken);
            foreach (var account in accounts)
                await _accounts.SaveAsync(account, cancellationToken);
            foreach (var claim in claims)
                _claims.Save(claim);
            if (hop is not null)
                _hops.Save(hop);
        }
    }
}
