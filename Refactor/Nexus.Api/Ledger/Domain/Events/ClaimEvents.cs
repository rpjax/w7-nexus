namespace Refactor.Nexus.Api.Ledger.Domain.Events;

public sealed record ClaimOpened(
    Guid ClaimId,
    Guid BeneficiaryId,
    decimal Amount,
    string Currency,
    Guid OriginChargeId,
    Guid LocationAccountId,
    string Kind,
    DateTime OccurredAt,
    Guid? ParentClaimId = null);

public sealed record ClaimAdjusted(
    Guid ClaimId,
    decimal Amount,
    string Currency,
    Guid LocationAccountId,
    DateTime OccurredAt);

public sealed record ClaimArchived(Guid ClaimId, DateTime OccurredAt);

public sealed record ClaimRepassed(Guid ClaimId, DateTime OccurredAt);

public sealed record HopDestinationSnapshot(Guid AccountId, decimal Amount, string Currency);

public sealed record HopRegistered(
    Guid HopId,
    Guid OriginAccountId,
    string OriginCurrency,
    Guid[] BundleClaimIds,
    HopDestinationSnapshot[] Destinations,
    Guid? CutOrangeMemberId,
    decimal? CutPercent,
    bool CutInPlace,
    decimal LossAmount,
    DateTime OccurredAt);
