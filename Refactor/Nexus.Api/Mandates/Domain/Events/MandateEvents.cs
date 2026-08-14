using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Domain.Events;

public sealed record MandateOpened(Guid MemberId, DateTime OccurredAt, Guid? ActedBy);

public sealed record MandateBackfilled(Guid MemberId, MandateGrantDto[] Grants, string[] Presets);

public sealed record MandatePresetGranted(Guid MemberId, string PresetId, Guid GrantedBy, MandateGrantDto[] AddedGrants, DateTime OccurredAt, Guid? ActedBy);

public sealed record MandatePresetRevoked(Guid MemberId, string PresetId, DateTime OccurredAt, Guid? ActedBy);

public sealed record MandateCapabilityGranted(
    Guid MemberId,
    MandateGrantDto Grant,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record MandateCapabilityRevoked(Guid MemberId, string Capability, string ScopeJson, DateTime OccurredAt, Guid? ActedBy);

public sealed record MandateGrantsPruned(Guid MemberId, Guid[] RemovedGrantIds, string[] RemainingPresets, DateTime OccurredAt, Guid? ActedBy);

public sealed record MandateGrantDto(
    Guid Id,
    string Capability,
    string ScopeJson,
    Guid GrantedBy,
    DateTime GrantedAt,
    string? SourcePreset);

public sealed record AgencyDealOpened(
    Guid DealId,
    Guid RecruiterId,
    Guid OperatorId,
    decimal OperatorPercent,
    decimal RecruiterPercent,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record AgencyDealBackfilled(
    Guid DealId,
    Guid RecruiterId,
    Guid OperatorId,
    decimal OperatorPercent,
    decimal RecruiterPercent,
    string Status,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record AgencyDealRatesChanged(
    Guid DealId,
    Guid RecruiterId,
    decimal OperatorPercent,
    decimal RecruiterPercent,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record AgencyDealClosed(Guid DealId, DateTime OccurredAt, Guid? ActedBy);

public sealed record ShareholderStakeSet(Guid AccountId, decimal Percentage, DateTime OccurredAt, Guid? ActedBy);

public sealed record ShareholderStakeRemoved(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
