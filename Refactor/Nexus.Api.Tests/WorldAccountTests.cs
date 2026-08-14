using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Tests.Fakes;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Ledger;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using Refactor.Nexus.Api.WorldAccounts.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Domain.Events;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Tests;

public sealed class WorldAccountDomainTests
{
    [Fact]
    public void Credit_brl_does_not_create_usd_balance()
    {
        var account = OpenBank();
        Assert.True(account.Credit("BRL", 10).IsSuccess);
        Assert.Equal(10, account.BalanceOf("BRL"));
        Assert.Equal(0, account.BalanceOf("USD"));
        Assert.False(account.Balances.ContainsKey("USD"));
    }

    [Fact]
    public void Gateway_open_requires_orange_on_aggregate()
    {
        var opened = WorldAccountAggregate.Open(WorldAccountKind.Gateway, "gw", null, 10, "BRL", 100);
        Assert.True(opened.IsFailure);
        Assert.Equal(WorldAccountErrorCodes.OrangeRequired, opened.Errors.First().Code);
    }

    [Fact]
    public void Bank_cannot_carry_orange()
    {
        var opened = WorldAccountAggregate.Open(WorldAccountKind.Bank, "bank", Guid.NewGuid(), null, "BRL", 10);
        Assert.True(opened.IsFailure);
        Assert.Equal(WorldAccountErrorCodes.OrangeNotAllowed, opened.Errors.First().Code);
    }

    [Fact]
    public void Bank_and_payout_cannot_emit()
    {
        var bank = WorldAccountAggregate.Open(WorldAccountKind.Bank, "bank", null, null, "BRL", 1000).Value!;
        var payout = WorldAccountAggregate.Open(WorldAccountKind.Payout, "out", null, null, "BRL", 1000).Value!;
        Assert.False(bank.CanEmit("BRL", 10));
        Assert.False(payout.CanEmit("BRL", 10));
    }

    [Fact]
    public void Blocked_emission_does_not_emit()
    {
        var account = OpenGateway(100);
        Assert.True(account.SetEmissionStatus(EmissionStatus.Blocked).IsSuccess);
        Assert.False(account.CanEmit("BRL", 10));
        Assert.Equal(WorldAccountErrorCodes.EmissionBlocked, account.ConsumeQuota("BRL", 10).Errors.First().Code);
    }

    [Fact]
    public void Frozen_balance_still_emits_when_quota_ok()
    {
        var account = OpenGateway(100);
        Assert.True(account.SetBalanceStatus(BalanceStatus.Frozen).IsSuccess);
        Assert.True(account.CanEmit("BRL", 10));
    }

    [Fact]
    public void Lost_account_cannot_change_emission_or_unfreeze()
    {
        var account = OpenGateway(100);
        Assert.True(account.SetBalanceStatus(BalanceStatus.Lost).IsSuccess);
        Assert.Equal(WorldAccountErrorCodes.BalanceLost, account.SetEmissionStatus(EmissionStatus.Blocked).Errors.First().Code);
        Assert.Equal(WorldAccountErrorCodes.BalanceLost, account.SetBalanceStatus(BalanceStatus.Frozen).Errors.First().Code);
        Assert.Equal(WorldAccountErrorCodes.BalanceLost, account.SetBalanceStatus(BalanceStatus.Accessible).Errors.First().Code);
    }

    [Fact]
    public void Consume_quota_persists_on_replay()
    {
        var live = OpenGateway(50);
        Assert.True(live.ConsumeQuota("BRL", 20).IsSuccess);
        var replayed = EventFold.Replay<WorldAccountAggregate>(live.UncommittedEvents);
        Assert.Equal(30, replayed.QuotaOf("BRL"));
        Assert.Contains(live.UncommittedEvents, e => e is QuotaConsumed);
    }

    private static WorldAccountAggregate OpenBank() =>
        WorldAccountAggregate.Open(WorldAccountKind.Bank, "bank", null, null, null, null).Value!;

    private static WorldAccountAggregate OpenGateway(decimal quota) =>
        WorldAccountAggregate.Open(WorldAccountKind.Gateway, "gw", Guid.NewGuid(), 10, "BRL", quota).Value!;
}

public sealed class WorldAccountUseCaseTests
{
    [Fact]
    public async Task Open_gateway_fails_when_orange_not_eligible()
    {
        var handler = new OpenWorldAccountHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: false),
            new Store(),
            new RecordingJournalWriter());

        var result = await handler.HandleAsync(new OpenWorldAccountCommand(
            "Gateway", "gw", Guid.NewGuid().ToString(), 10, "BRL", 100));

        Assert.True(result.IsFailure);
        Assert.Equal(WorldAccountErrorCodes.OrangeNotEligible, result.Errors.First().Code);
    }

    [Fact]
    public async Task Open_gateway_succeeds_for_eligible_orange()
    {
        var orange = Guid.NewGuid();
        var store = new Store();
        var handler = new OpenWorldAccountHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: true),
            store,
            new RecordingJournalWriter());

        var result = await handler.HandleAsync(new OpenWorldAccountCommand(
            "Gateway", "gw", orange.ToString(), 12, "BRL", 80));

        Assert.True(result.IsSuccess);
        var loaded = await store.GetByIdAsync(result.Value!.AccountId);
        Assert.NotNull(loaded);
        Assert.Equal(WorldAccountKind.Gateway, loaded!.Kind);
        Assert.Equal(80, loaded.QuotaOf("BRL"));
        Assert.Equal(0, loaded.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Configure_lost_is_rejected()
    {
        var orange = Guid.NewGuid();
        var store = new Store();
        var opened = await new OpenWorldAccountHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: true),
            store,
            new RecordingJournalWriter()).HandleAsync(new OpenWorldAccountCommand(
            "Gateway", "gw", orange.ToString(), 12, "BRL", 80));
        Assert.True(opened.IsSuccess);

        var result = await new ConfigureWorldAccountHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: true),
            store,
            new RecordingJournalWriter()).HandleAsync(new ConfigureWorldAccountCommand(
            opened.Value!.AccountId.ToString(),
            null, null, null, null, null, "Lost"));

        Assert.True(result.IsFailure);
        Assert.Equal(WorldAccountErrorCodes.UseLostEndpoint, result.Errors.First().Code);
    }

    [Fact]
    public async Task Observation_on_lost_account_is_rejected()
    {
        var store = new Store();
        var opened = await OpenBankAsync(store);
        var account = await store.GetByIdAsync(opened);
        Assert.True(account!.SetBalanceStatus(BalanceStatus.Lost).IsSuccess);
        await store.SaveAsync(account);

        var result = await Observe(store, opened, new EmptyLedger(), "credit", "BRL", 10);
        Assert.False(result.Ok);
        Assert.Equal(WorldAccountErrorCodes.BalanceLost, result.Code);
    }

    [Fact]
    public async Task Observation_seed_without_claims_succeeds()
    {
        var store = new Store();
        var opened = await OpenBankAsync(store);
        var result = await Observe(store, opened, new EmptyLedger(), "credit", "BRL", 40);
        Assert.True(result.Ok);
        var loaded = await store.GetByIdAsync(opened);
        Assert.Equal(40, loaded!.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Observation_with_active_claims_is_rejected()
    {
        var store = new Store();
        var opened = await OpenBankAsync(store);
        var result = await Observe(store, opened, new StubLedger(hasAny: true, hasActive: true), "credit", "BRL", 10);
        Assert.False(result.Ok);
        Assert.Equal(WorldAccountErrorCodes.ObservationSeedOnly, result.Code);
        var loaded = await store.GetByIdAsync(opened);
        Assert.Equal(0, loaded!.BalanceOf("BRL"));
    }

    [Fact]
    public async Task Observation_after_terminal_claims_is_still_rejected()
    {
        var store = new Store();
        var opened = await OpenBankAsync(store);
        var result = await Observe(store, opened, new StubLedger(hasAny: true, hasActive: false), "debit", "BRL", 5);
        Assert.False(result.Ok);
        Assert.Equal(WorldAccountErrorCodes.ObservationSeedOnly, result.Code);
    }

    [Fact]
    public async Task Observation_is_allowed_when_claims_exist_only_in_another_currency()
    {
        var store = new Store();
        var opened = await OpenBankAsync(store);
        var ledger = new CurrencyLedger("USD");
        var result = await Observe(store, opened, ledger, "credit", "BRL", 15);
        Assert.True(result.Ok);
        Assert.Equal("BRL", ledger.LastCurrency);
        Assert.Equal(opened, ledger.LastAccountId);
        var loaded = await store.GetByIdAsync(opened);
        Assert.Equal(15, loaded!.BalanceOf("BRL"));
    }

    private static async Task<Guid> OpenBankAsync(Store store)
    {
        var opened = await new OpenWorldAccountHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: true),
            store,
            new RecordingJournalWriter()).HandleAsync(new OpenWorldAccountCommand("Bank", "bank", null, null, null, null));
        Assert.True(opened.IsSuccess);
        return opened.Value!.AccountId;
    }

    private static async Task<(bool Ok, string? Code)> Observe(
        Store store,
        Guid accountId,
        ILedgerClaimObservationPort ledger,
        string direction,
        string currency,
        decimal amount)
    {
        var result = await new RecordWorldAccountObservationHandler(
            new AdminContext(Guid.NewGuid()),
            new Access(eligibleOrange: true),
            store,
            ledger,
            new RecordingJournalWriter()).HandleAsync(new RecordWorldAccountObservationCommand(
            accountId.ToString(), direction, currency, amount, "seed"));
        return (result.IsSuccess, result.Errors.FirstOrDefault()?.Code);
    }

    private sealed class EmptyLedger : ILedgerClaimObservationPort
    {
        public Task<LedgerClaimPresence> GetPresenceAsync(
            Guid worldAccountId,
            string currency,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LedgerClaimPresence(false, false));
    }

    private sealed class StubLedger : ILedgerClaimObservationPort
    {
        private readonly LedgerClaimPresence _presence;
        public StubLedger(bool hasAny, bool hasActive) => _presence = new LedgerClaimPresence(hasAny, hasActive);

        public Task<LedgerClaimPresence> GetPresenceAsync(
            Guid worldAccountId,
            string currency,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_presence);
    }

    private sealed class CurrencyLedger : ILedgerClaimObservationPort
    {
        private readonly string _blockedCurrency;
        public CurrencyLedger(string blockedCurrency) => _blockedCurrency = blockedCurrency;
        public string? LastCurrency { get; private set; }
        public Guid LastAccountId { get; private set; }

        public Task<LedgerClaimPresence> GetPresenceAsync(
            Guid worldAccountId,
            string currency,
            CancellationToken cancellationToken = default)
        {
            LastCurrency = currency;
            LastAccountId = worldAccountId;
            var blocked = string.Equals(currency, _blockedCurrency, StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(new LedgerClaimPresence(blocked, blocked));
        }
    }

    private sealed class Access : IWorldAccountAccess
    {
        private readonly bool _eligible;
        public Access(bool eligibleOrange) => _eligible = eligibleOrange;

        public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> CanManageGatewaysAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsEligibleOrangeAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_eligible);
    }

    private sealed class AdminContext : IRequestContext
    {
        private readonly Guid _id;
        public AdminContext(Guid id) => _id = id;

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(
                new RequesterContext(_id.ToString(), [Roles.Administrator], [])));
    }

    private sealed class Store : IWorldAccountRepository
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
}
