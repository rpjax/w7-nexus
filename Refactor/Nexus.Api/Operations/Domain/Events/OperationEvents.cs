namespace Refactor.Nexus.Api.Operations.Domain.Events;

public sealed record OperationOpened(
    Guid OperationId,
    string Key,
    string Name,
    decimal? ManagementCutPercent,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record OperationBackfilled(
    Guid OperationId,
    string Key,
    string Name,
    string Status,
    decimal? ManagementCutPercent,
    Guid[] AssignedOperatorIds,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record OperationTransitioned(Guid OperationId, string From, string To, DateTime OccurredAt, Guid? ActedBy);

public sealed record OperationAssignmentsCleared(Guid OperationId, DateTime OccurredAt, Guid? ActedBy);

public sealed record OperationManagementCutConfigured(
    Guid OperationId,
    decimal? ManagementCutPercent,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record OperatorAssigned(Guid OperationId, Guid MemberId, DateTime OccurredAt, Guid? ActedBy);

public sealed record OperatorUnassigned(Guid OperationId, Guid MemberId, DateTime OccurredAt, Guid? ActedBy);
