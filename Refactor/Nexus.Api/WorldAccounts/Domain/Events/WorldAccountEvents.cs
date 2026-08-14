namespace Refactor.Nexus.Api.WorldAccounts.Domain.Events;

public sealed record CurrencyAmount(string Currency, decimal Amount);

public sealed record WorldAccountOpened(
    Guid AccountId,
    string Kind,
    string Label,
    Guid? OrangeMemberId,
    decimal? Level1CutPercent,
    CurrencyAmount[] InitialQuotas,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record WorldAccountBackfilled(
    Guid AccountId,
    string Kind,
    string Label,
    Guid? OrangeMemberId,
    decimal? Level1CutPercent,
    string EmissionStatus,
    string BalanceStatus,
    CurrencyAmount[] Quotas,
    CurrencyAmount[] Balances,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record WorldAccountLabeled(Guid AccountId, string Label, DateTime OccurredAt, Guid? ActedBy);

public sealed record GatewayCutConfigured(Guid AccountId, decimal Level1CutPercent, DateTime OccurredAt, Guid? ActedBy);

public sealed record GatewayOrangeChanged(Guid AccountId, Guid OrangeMemberId, DateTime OccurredAt, Guid? ActedBy);

public sealed record EmissionStatusChanged(Guid AccountId, string Status, DateTime OccurredAt, Guid? ActedBy);

public sealed record BalanceStatusChanged(Guid AccountId, string Status, DateTime OccurredAt, Guid? ActedBy);

public sealed record QuotaConfigured(Guid AccountId, string Currency, decimal Remaining, DateTime OccurredAt, Guid? ActedBy);

public sealed record QuotaConsumed(
    Guid AccountId,
    string Currency,
    decimal Amount,
    Guid? ChargeId,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record ObservedCredited(
    Guid AccountId,
    string Currency,
    decimal Amount,
    string? Memo,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record ObservedDebited(
    Guid AccountId,
    string Currency,
    decimal Amount,
    string? Memo,
    DateTime OccurredAt,
    Guid? ActedBy);
