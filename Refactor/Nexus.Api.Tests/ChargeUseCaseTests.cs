using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Issuing;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Charging.Application.UseCases.Authenticated.Commands;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.Events;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class ChargeUseCaseTests
{
    [Fact]
    public async Task Create_fails_when_operation_not_active()
    {
        var fixture = Fixture.Create(active: false, assigned: true);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 50, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.OperationNotActive, result.Errors.First().Code);
    }

    [Fact]
    public async Task Create_fails_when_operator_not_assigned()
    {
        var fixture = Fixture.Create(active: true, assigned: false);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 50, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.OperatorNotAssigned, result.Errors.First().Code);
        Assert.Contains("associado", result.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_create_without_operator_member_fails()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 50, "BRL", null, null));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.OperatorNotAssigned, result.Errors.First().Code);
        Assert.Contains("escolha", result.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mark_paid_succeeds_when_journal_throws_after_save()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);

        var result = await new MarkChargePaidHandler(
            new AdminRequestContext(fixture.AdminId),
            fixture.Charges,
            new ThrowingJournalWriter()).HandleAsync(
            new MarkChargePaidCommand(created.Value!.ChargeId.ToString(), null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Paid", result.Value!.Status);
        var reloaded = await fixture.Charges.GetByIdAsync(created.Value.ChargeId);
        Assert.Equal(ChargeStatus.Paid, reloaded!.Status);
    }

    [Fact]
    public async Task Create_fails_without_quota()
    {
        var fixture = Fixture.Create(active: true, assigned: true, quota: 10);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 50, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.NoQuota, result.Errors.First().Code);
    }

    [Fact]
    public async Task Override_outside_set_fails()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 10, "BRL", Guid.NewGuid().ToString(), fixture.OperatorId.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.RailNotInSet, result.Errors.First().Code);
    }

    [Fact]
    public async Task Snapshot_does_not_change_when_deal_updates()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 40, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);

        fixture.Agency = new AgencySlice(fixture.OperatorId, 10, fixture.RecruiterId, 10);
        var charge = await fixture.Charges.GetByIdAsync(created.Value!.ChargeId);
        Assert.NotNull(charge);
        var agencyLine = charge!.SplitIntent.Lines.Single(l => l.Kind == "Agency");
        Assert.Equal(80m, agencyLine.PercentOfRemainder);
    }

    [Fact]
    public async Task Paid_is_idempotent_and_does_not_invent_claims()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        var charge = (await fixture.Charges.GetByIdAsync(created.Value!.ChargeId))!;
        Assert.True(charge.MarkPaid().IsSuccess);
        await fixture.Charges.SaveAsync(charge);
        var again = (await fixture.Charges.GetByIdAsync(charge.Id))!;
        Assert.True(again.MarkPaid().IsSuccess);
        Assert.Equal(ChargeStatus.Paid, again.Status);
        Assert.Equal(1, fixture.Issuer.Calls);
    }

    [Fact]
    public async Task Orange_cut_comes_from_selected_rail()
    {
        var fixture = Fixture.Create(active: true, assigned: true, orangeCut: 12.5m);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        var charge = await fixture.Charges.GetByIdAsync(created.Value!.ChargeId);
        Assert.Equal(fixture.OrangeId, charge!.OrangeMemberId);
        Assert.Equal(12.5m, charge.SplitIntent.Lines[0].PercentOfRemainder);
    }

    [Fact]
    public async Task Create_consumes_quota_on_reload()
    {
        var fixture = Fixture.Create(active: true, assigned: true, quota: 100);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 40, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);
        var reloaded = await fixture.Accounts.GetByIdAsync(fixture.GatewayId);
        Assert.Equal(60, reloaded!.QuotaOf("BRL"));
    }

    [Fact]
    public async Task Bound_bank_is_not_in_emit_pool()
    {
        var fixture = Fixture.Create(active: true, assigned: true, quota: 1000, bindGateway: false);
        var bank = WorldAccountAggregate.Open(WorldAccountKind.Bank, "bank", null, null, "BRL", 1000).Value!;
        await fixture.Accounts.SaveAsync(bank);
        await fixture.Sets.BindAsync(fixture.OperationId, bank.Id);
        var result = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 10, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(ChargingErrorCodes.NoQuota, result.Errors.First().Code);
    }

    [Fact]
    public async Task Operator_list_omits_split_and_orange()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);

        var listed = await new ListChargesHandler(
            new MemberRequestContext(fixture.OperatorId),
            new StubMandates(fixture),
            fixture.Charges).HandleAsync(new ListChargesQuery(null, null));
        var view = Assert.Single(listed.Value!.Items);
        Assert.Null(view.SplitIntent);
        Assert.Null(view.OrangeMemberId);
        Assert.Equal(20, view.GrossAmount);

        var adminListed = await new ListChargesHandler(
            new AdminRequestContext(fixture.AdminId),
            new StubMandates(fixture),
            fixture.Charges).HandleAsync(new ListChargesQuery(null, null));
        var adminView = Assert.Single(adminListed.Value!.Items);
        Assert.NotNull(adminView.SplitIntent);
        Assert.Equal(fixture.OrangeId, adminView.OrangeMemberId);
    }

    [Fact]
    public async Task Operator_get_omits_split_and_orange()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);

        var got = await new GetChargeHandler(
            new MemberRequestContext(fixture.OperatorId),
            new StubMandates(fixture),
            fixture.Charges).HandleAsync(new GetChargeQuery(created.Value!.ChargeId.ToString()));
        Assert.True(got.IsSuccess);
        Assert.Null(got.Value!.SplitIntent);
        Assert.Null(got.Value.OrangeMemberId);
        Assert.Equal(20, got.Value.GrossAmount);
    }

    [Fact]
    public async Task Operator_cannot_get_another_operators_charge()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        Assert.True(created.IsSuccess);

        var got = await new GetChargeHandler(
            new MemberRequestContext(Guid.NewGuid()),
            new StubMandates(fixture),
            fixture.Charges).HandleAsync(new GetChargeQuery(created.Value!.ChargeId.ToString()));
        Assert.False(got.IsAuthorized);
    }

    [Fact]
    public async Task Accountant_get_includes_split()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var created = await fixture.Handler.HandleAsync(new CreateChargeCommand(
            fixture.OperationId.ToString(), 20, "BRL", null, fixture.OperatorId.ToString()));
        var accountant = Guid.NewGuid();

        var got = await new GetChargeHandler(
            new MemberRequestContext(accountant),
            new StubMandates(fixture, splitViewerId: accountant),
            fixture.Charges).HandleAsync(new GetChargeQuery(created.Value!.ChargeId.ToString()));
        Assert.True(got.IsSuccess);
        Assert.NotNull(got.Value!.SplitIntent);
        Assert.Equal(fixture.OrangeId, got.Value.OrangeMemberId);
    }

    [Fact]
    public async Task Gateways_can_bind_rail_without_admin_role()
    {
        var fixture = Fixture.Create(active: true, assigned: true);
        var gatewayUser = Guid.NewGuid();
        var mandates = new StubMandates(fixture, railManagerId: gatewayUser);
        var bind = await new BindEmissionRailHandler(
            new MemberRequestContext(gatewayUser),
            mandates,
            fixture.Accounts,
            fixture.Sets,
            new StubOperations(fixture.OperationId, true, true),
            new RecordingJournalWriter()).HandleAsync(
            new BindEmissionRailCommand(fixture.OperationId.ToString(), fixture.GatewayId.ToString()));
        Assert.True(bind.IsSuccess);

        var denied = await new BindEmissionRailHandler(
            new MemberRequestContext(fixture.OperatorId),
            mandates,
            fixture.Accounts,
            fixture.Sets,
            new StubOperations(fixture.OperationId, true, true),
            new RecordingJournalWriter()).HandleAsync(
            new BindEmissionRailCommand(fixture.OperationId.ToString(), fixture.GatewayId.ToString()));
        Assert.False(denied.IsAuthorized);
    }

    private sealed class Fixture
    {
        public Guid OperationId { get; } = Guid.NewGuid();
        public Guid OperatorId { get; } = Guid.NewGuid();
        public Guid RecruiterId { get; } = Guid.NewGuid();
        public Guid OrangeId { get; } = Guid.NewGuid();
        public Guid AdminId { get; } = Guid.NewGuid();
        public InMemoryChargeRepository Charges { get; } = new();
        public InMemoryWorldAccountStore Accounts { get; } = new();
        public InMemoryEmissionSet Sets { get; } = new();
        public Guid GatewayId { get; }
        public RecordingIssuer Issuer { get; } = new();
        public AgencySlice Agency { get; set; }
        public CreateChargeHandler Handler { get; }

        private Fixture(bool active, bool assigned, decimal quota, decimal orangeCut, bool bindGateway)
        {
            Agency = new AgencySlice(OperatorId, 80, RecruiterId, 0);
            if (bindGateway)
            {
                var opened = WorldAccountAggregate.Open(
                    WorldAccountKind.Gateway,
                    "gw",
                    OrangeId,
                    orangeCut,
                    "BRL",
                    quota).Value!;
                Accounts.SaveAsync(opened).GetAwaiter().GetResult();
                Sets.BindAsync(OperationId, opened.Id).GetAwaiter().GetResult();
                GatewayId = opened.Id;
            }

            Handler = new CreateChargeHandler(
                new AdminRequestContext(AdminId),
                new StubMandates(this),
                new StubOperations(OperationId, active, assigned),
                Accounts,
                Sets,
                Charges,
                Issuer,
                new RecordingJournalWriter());
        }

        public static Fixture Create(
            bool active,
            bool assigned,
            decimal quota = 1000,
            decimal orangeCut = 10,
            bool bindGateway = true) =>
            new(active, assigned, quota, orangeCut, bindGateway);
    }

    private sealed class StubOperations : IOperationChargingDirectory
    {
        private readonly Guid _operationId;
        private readonly bool _active;
        private readonly bool _assigned;

        public StubOperations(Guid operationId, bool active, bool assigned)
        {
            _operationId = operationId;
            _active = active;
            _assigned = assigned;
        }

        public Task<OperationChargingSnapshot?> GetAsync(Guid operationId, Guid operatorMemberId, CancellationToken cancellationToken = default)
        {
            if (operationId != _operationId)
                return Task.FromResult<OperationChargingSnapshot?>(null);
            return Task.FromResult<OperationChargingSnapshot?>(
                new OperationChargingSnapshot(operationId, _active, 5, _assigned));
        }
    }

    private sealed class StubMandates : IChargingMandateSnapshot
    {
        private readonly Fixture _fixture;
        private readonly Guid? _railManagerId;
        private readonly Guid? _splitViewerId;
        public StubMandates(Fixture fixture, Guid? railManagerId = null, Guid? splitViewerId = null)
        {
            _fixture = fixture;
            _railManagerId = railManagerId;
            _splitViewerId = splitViewerId;
        }

        public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(accountId == _fixture.AdminId);

        public Task<bool> CanManageRailsAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(accountId == _fixture.AdminId || accountId == _railManagerId);

        public Task<bool> CanSeeChargeSplitAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(accountId == _fixture.AdminId || accountId == _splitViewerId);

        public Task<bool> CanManageOperationsAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            IsAdministratorAsync(accountId, cancellationToken);

        public Task<bool> IsEligibleOrangeAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(accountId == _fixture.OrangeId);

        public Task<MandateChargingSnapshot?> CaptureAsync(Guid operatorMemberId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MandateChargingSnapshot?>(new MandateChargingSnapshot(
                _fixture.Agency,
                [new ShareholderSlice(Guid.NewGuid(), 15)]));
    }

    private sealed class MemberRequestContext : IRequestContext
    {
        private readonly Guid _id;
        public MemberRequestContext(Guid id) => _id = id;

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(
                new RequesterContext(_id.ToString(), [], [])));
    }

    private sealed class AdminRequestContext : IRequestContext
    {
        private readonly Guid _id;
        public AdminRequestContext(Guid id) => _id = id;

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(
                new RequesterContext(_id.ToString(), [Roles.Administrator], [])));
    }

    private sealed class ThrowingJournalWriter : IJournalWriter
    {
        public void Append<T>(T payload) =>
            throw new InvalidOperationException("Journal fact 'Charging.ChargeTransitioned' requires index key 'member'.");
    }

    private sealed class RecordingIssuer : IPaymentIssuer
    {
        public int Calls { get; private set; }

        public Task<PaymentIssueResult> IssueAsync(Guid chargeId, decimal grossAmount, string currency, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new PaymentIssueResult($"noop-{chargeId:N}"));
        }
    }

    private sealed class InMemoryChargeRepository : IChargeRepository
    {
        private readonly Dictionary<Guid, List<object>> _streams = [];

        public Task<ChargeAggregate?> GetByIdAsync(Guid chargeId, CancellationToken cancellationToken = default)
        {
            if (!_streams.TryGetValue(chargeId, out var events))
                return Task.FromResult<ChargeAggregate?>(null);
            return Task.FromResult<ChargeAggregate?>(Replay(events));
        }

        public Task<ChargeAggregate?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
        {
            foreach (var events in _streams.Values)
            {
                var charge = Replay(events);
                if (charge.ExternalReference == externalReference)
                    return Task.FromResult<ChargeAggregate?>(charge);
            }

            return Task.FromResult<ChargeAggregate?>(null);
        }

        public Task SaveAsync(ChargeAggregate charge, CancellationToken cancellationToken = default)
        {
            if (!_streams.TryGetValue(charge.Id, out var events))
            {
                events = [];
                _streams[charge.Id] = events;
            }

            events.AddRange(charge.UncommittedEvents);
            charge.ClearUncommitted();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChargeAggregate>> ListAsync(
            Guid? operationId,
            Guid? operatorMemberId,
            CancellationToken cancellationToken = default)
        {
            var items = _streams.Values.Select(Replay).AsEnumerable();
            if (operationId is not null)
                items = items.Where(c => c.OperationId == operationId);
            if (operatorMemberId is not null)
                items = items.Where(c => c.OperatorMemberId == operatorMemberId);
            return Task.FromResult<IReadOnlyList<ChargeAggregate>>(items.ToList());
        }

        private static ChargeAggregate Replay(IEnumerable<object> events)
        {
            var charge = new ChargeAggregate();
            foreach (var @event in events)
            {
                switch (@event)
                {
                    case ChargeOpened opened:
                        charge.Apply(opened);
                        break;
                    case ChargeExternalReferenceAssigned assigned:
                        charge.Apply(assigned);
                        break;
                    case ChargePaid paid:
                        charge.Apply(paid);
                        break;
                    case ChargeCancelled cancelled:
                        charge.Apply(cancelled);
                        break;
                    case ChargeExpired expired:
                        charge.Apply(expired);
                        break;
                    case ChargeFailed failed:
                        charge.Apply(failed);
                        break;
                    case ChargeMaterialized materialized:
                        charge.Apply(materialized);
                        break;
                }
            }

            return charge;
        }
    }

    internal sealed class InMemoryWorldAccountStore : IWorldAccountRepository
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

    internal sealed class InMemoryEmissionSet : IOperationEmissionSetRepository
    {
        private readonly Dictionary<Guid, HashSet<Guid>> _sets = [];

        public Task<IReadOnlyList<Guid>> ListRailIdsAsync(Guid operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                _sets.TryGetValue(operationId, out var set) ? set.ToList() : []);

        public Task BindAsync(Guid operationId, Guid railId, CancellationToken cancellationToken = default)
        {
            if (!_sets.TryGetValue(operationId, out var set))
            {
                set = [];
                _sets[operationId] = set;
            }

            set.Add(railId);
            return Task.CompletedTask;
        }

        public Task UnbindAsync(Guid operationId, Guid railId, CancellationToken cancellationToken = default)
        {
            if (_sets.TryGetValue(operationId, out var set))
                set.Remove(railId);
            return Task.CompletedTask;
        }
    }
}
