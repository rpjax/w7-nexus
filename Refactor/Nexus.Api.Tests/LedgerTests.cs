using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.Events;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Events;
using Refactor.Nexus.Api.Ledger.Domain.Services;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class WaterfallMaterializerTests
{
    [Fact]
    public void Remainder_under_100_goes_to_organization()
    {
        var orange = Guid.NewGuid();
        var op = Guid.NewGuid();
        var intent = SplitIntentFactory.Create(
            orange,
            10,
            [],
            0,
            new AgencySlice(op, 70, Guid.NewGuid(), 0)).Value!;

        var slices = WaterfallMaterializer.Allocate(intent, 100);
        Assert.Equal(100, slices.Sum(s => s.Amount));
        Assert.Equal(10, slices.Single(s => s.Kind == SplitIntent.Orange).Amount);
        Assert.Equal(63, slices.Single(s => s.Kind == SplitIntent.Agency).Amount);
        Assert.Equal(27, slices.Single(s => s.BeneficiaryId == OrganizationParty.Id).Amount);
    }

    [Fact]
    public void Rounding_dust_goes_to_residual()
    {
        var orange = Guid.NewGuid();
        var intent = SplitIntentFactory.Create(
            orange,
            33.33m,
            [],
            0,
            new AgencySlice(Guid.NewGuid(), 0, Guid.NewGuid(), 0)).Value!;

        var slices = WaterfallMaterializer.Allocate(intent, 100);
        Assert.Equal(100, slices.Sum(s => s.Amount));
        Assert.Equal(33.33m, slices.Single(s => s.Kind == SplitIntent.Orange).Amount);
        Assert.Equal(66.67m, slices.Single(s => s.BeneficiaryId == OrganizationParty.Id).Amount);
    }

    [Fact]
    public void Empty_management_line_goes_to_organization()
    {
        var orange = Guid.NewGuid();
        var intent = SplitIntentFactory.Create(
            orange,
            0,
            [],
            20,
            new AgencySlice(Guid.NewGuid(), 0, Guid.NewGuid(), 0)).Value!;

        var slices = WaterfallMaterializer.Allocate(intent, 50);
        Assert.Equal(50, slices.Sum(s => s.Amount));
        Assert.Equal(10, slices.Single(s => s.Kind == SplitIntent.OperationManagement).Amount);
        Assert.Equal(OrganizationParty.Id, slices.Single(s => s.Kind == SplitIntent.OperationManagement).BeneficiaryId);
    }

    [Fact]
    public void Zero_recruiter_does_not_create_slice()
    {
        var recruiter = Guid.NewGuid();
        var intent = SplitIntentFactory.Create(
            Guid.NewGuid(),
            0,
            [],
            0,
            new AgencySlice(Guid.NewGuid(), 40, recruiter, 0)).Value!;

        var slices = WaterfallMaterializer.Allocate(intent, 10);
        Assert.DoesNotContain(slices, s => s.BeneficiaryId == recruiter);
        Assert.Equal(10, slices.Sum(s => s.Amount));
    }
}

public sealed class MaterializeChargeTests
{
    [Fact]
    public async Task Creates_claims_that_sum_to_net_and_credits_account()
    {
        var fx = await Fixture.PaidAsync();
        var result = await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString()));
        Assert.True(result.IsSuccess);
        var claims = await fx.Claims.ListAsync(fx.Charge.Id, null, null);
        Assert.Equal(80, claims.Sum(c => c.Amount));
        var landing = await fx.Accounts.GetByIdAsync(fx.Landing.Id);
        Assert.Equal(80, landing!.BalanceOf("BRL"));
        Assert.Equal(ChargeStatus.Materialized, (await fx.Charges.GetByIdAsync(fx.Charge.Id))!.Status);
    }

    [Fact]
    public async Task Second_call_with_same_payload_is_idempotent()
    {
        var fx = await Fixture.PaidAsync();
        var cmd = new MaterializeChargeCommand(fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString());
        Assert.True((await fx.Handler.HandleAsync(cmd)).IsSuccess);
        var second = await fx.Handler.HandleAsync(cmd);
        Assert.True(second.IsSuccess);
        var claims = await fx.Claims.ListAsync(fx.Charge.Id, null, null);
        Assert.Equal(80, claims.Sum(c => c.Amount));
        Assert.Equal(80, (await fx.Accounts.GetByIdAsync(fx.Landing.Id))!.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Second_call_with_different_net_fails()
    {
        var fx = await Fixture.PaidAsync();
        Assert.True((await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString()))).IsSuccess);
        var second = await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 70, "BRL", fx.Landing.Id.ToString()));
        Assert.True(second.IsFailure);
        Assert.Equal(ChargingErrorCodes.AlreadyMaterialized, second.Errors.First().Code);
    }

    [Fact]
    public async Task Open_charge_cannot_materialize()
    {
        var fx = await Fixture.OpenAsync();
        var result = await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.NotPaid, result.Errors.First().Code);
    }

    [Fact]
    public async Task Lost_landing_is_rejected()
    {
        var fx = await Fixture.PaidAsync();
        fx.Landing.SetBalanceStatus(BalanceStatus.Lost);
        await fx.Accounts.SaveAsync(fx.Landing);
        var result = await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.LandingLost, result.Errors.First().Code);
    }

    [Fact]
    public async Task Orphan_world_balance_breaks_invariant()
    {
        var fx = await Fixture.PaidAsync();
        fx.Landing.Credit("BRL", 5, "orphan");
        await fx.Accounts.SaveAsync(fx.Landing);
        var result = await fx.Handler.HandleAsync(new MaterializeChargeCommand(
            fx.Charge.Id.ToString(), 80, "BRL", fx.Landing.Id.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(LedgerErrorCodes.InvariantBroken, result.Errors.First().Code);
    }

    private sealed class Fixture
    {
        public ChargeAggregate Charge { get; }
        public WorldAccountAggregate Landing { get; }
        public InMemoryCharges Charges { get; } = new();
        public InMemoryAccounts Accounts { get; } = new();
        public InMemoryClaims Claims { get; }
        public MaterializeChargeHandler Handler { get; }

        private Fixture(ChargeAggregate charge, WorldAccountAggregate landing)
        {
            Charge = charge;
            Landing = landing;
            Claims = new InMemoryClaims();
            Charges.SaveAsync(charge).GetAwaiter().GetResult();
            Accounts.SaveAsync(landing).GetAwaiter().GetResult();
            Handler = new MaterializeChargeHandler(
                new AdminContext(Guid.NewGuid()),
                new AllowAll(),
                Charges,
                Accounts,
                Claims,
                new Commit(Charges, Accounts, Claims),
                new RecordingJournalWriter());
        }

        public static Task<Fixture> OpenAsync() => Task.FromResult(new Fixture(OpenCharge(paid: false), OpenLanding()));

        public static Task<Fixture> PaidAsync() => Task.FromResult(new Fixture(OpenCharge(paid: true), OpenLanding()));

        private static ChargeAggregate OpenCharge(bool paid)
        {
            var orange = Guid.NewGuid();
            var intent = SplitIntentFactory.Create(
                orange,
                10,
                [],
                0,
                new AgencySlice(Guid.NewGuid(), 80, Guid.NewGuid(), 0)).Value!;
            var charge = ChargeAggregate.Open(Guid.NewGuid(), Guid.NewGuid(), 100, "BRL", Guid.NewGuid(), orange, intent).Value!;
            if (paid)
                charge.MarkPaid();
            return charge;
        }

        private static WorldAccountAggregate OpenLanding() =>
            WorldAccountAggregate.Open(WorldAccountKind.Bank, "land", null, null, null, null).Value!;
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

    private sealed class Commit : IMaterializationCommit
    {
        private readonly InMemoryCharges _charges;
        private readonly InMemoryAccounts _accounts;
        private readonly InMemoryClaims _claims;

        public Commit(InMemoryCharges charges, InMemoryAccounts accounts, InMemoryClaims claims)
        {
            _charges = charges;
            _accounts = accounts;
            _claims = claims;
        }

        public async Task SaveAsync(
            ChargeAggregate charge,
            WorldAccountAggregate account,
            IReadOnlyList<ClaimAggregate> claims,
            CancellationToken cancellationToken = default)
        {
            await _charges.SaveAsync(charge, cancellationToken);
            await _accounts.SaveAsync(account, cancellationToken);
            foreach (var claim in claims)
                _claims.Save(claim);
        }
    }
}
