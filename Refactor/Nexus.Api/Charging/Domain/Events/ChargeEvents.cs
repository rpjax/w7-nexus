using Refactor.Nexus.Api.Charging.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Charging.Domain.Events;

public sealed record ChargeOpened(
    Guid ChargeId,
    Guid OperationId,
    Guid OperatorMemberId,
    decimal GrossAmount,
    string Currency,
    Guid EmissionRailId,
    Guid OrangeMemberId,
    SplitIntent SplitIntent,
    DateTime OccurredAt);

public sealed record ChargeExternalReferenceAssigned(Guid ChargeId, string ExternalReference, DateTime OccurredAt);

public sealed record ChargePaid(Guid ChargeId, DateTime OccurredAt);

public sealed record ChargeCancelled(Guid ChargeId, DateTime OccurredAt);

public sealed record ChargeExpired(Guid ChargeId, DateTime OccurredAt);

public sealed record ChargeFailed(Guid ChargeId, DateTime OccurredAt);

public sealed record ChargeMaterialized(
    Guid ChargeId,
    decimal NetAmount,
    string Currency,
    Guid LandingWorldAccountId,
    DateTime OccurredAt);
