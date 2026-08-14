using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Mandates.Application.Journal;

[CanonicalFact("Mandates.MandatePresetGranted", schemaVersion: 1, Owner = "mandates", Name = "Mandate preset granted")]
public sealed class MandatePresetGranted
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string PresetId { get; init; }

    [JournalIndex("granted_by")]
    public required Guid GrantedBy { get; init; }
}

[CanonicalFact("Mandates.MandatePresetRevoked", schemaVersion: 1, Owner = "mandates", Name = "Mandate preset revoked")]
public sealed class MandatePresetRevoked
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string PresetId { get; init; }
}

[CanonicalFact("Mandates.MandateCapabilityGranted", schemaVersion: 1, Owner = "mandates", Name = "Mandate capability granted")]
public sealed class MandateCapabilityGranted
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Capability { get; init; }

    public required string ScopeKind { get; init; }
}

[CanonicalFact("Mandates.MandateCapabilityRevoked", schemaVersion: 1, Owner = "mandates", Name = "Mandate capability revoked")]
public sealed class MandateCapabilityRevoked
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Capability { get; init; }

    public required string ScopeKind { get; init; }
}

[CanonicalFact("Mandates.AgencyDealUpserted", schemaVersion: 1, Owner = "mandates", Name = "Agency deal upserted")]
public sealed class AgencyDealUpserted
{
    [JournalIndex("deal")]
    public required Guid DealId { get; init; }

    [JournalIndex("operator")]
    public required Guid OperatorId { get; init; }

    [JournalIndex("recruiter")]
    public required Guid RecruiterId { get; init; }

    public required decimal OperatorPercent { get; init; }
    public required decimal RecruiterPercent { get; init; }
}

[CanonicalFact("Mandates.AgencyDealClosed", schemaVersion: 1, Owner = "mandates", Name = "Agency deal closed")]
public sealed class AgencyDealClosed
{
    [JournalIndex("deal")]
    public required Guid DealId { get; init; }

    [JournalIndex("operator")]
    public required Guid OperatorId { get; init; }
}

[CanonicalFact("Mandates.CarteiraRead", schemaVersion: 1, Owner = "mandates", Name = "Carteira read")]
public sealed class MandateCarteiraRead
{
    [JournalIndex("member")]
    public required Guid MemberId { get; init; }
}

[CanonicalFact("Mandates.ShareholderStakeChanged", schemaVersion: 1, Owner = "mandates", Name = "Shareholder stake changed")]
public sealed class ShareholderStakeChanged
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required decimal Percentage { get; init; }

    public required string Change { get; init; }
}
