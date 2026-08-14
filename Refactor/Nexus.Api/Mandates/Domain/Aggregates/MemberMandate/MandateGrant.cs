using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;

public sealed class MandateGrant
{
    public MandateGrant(
        Guid id,
        string capability,
        MandateScope scope,
        MemberId grantedBy,
        DateTime grantedAt,
        string? sourcePreset)
    {
        Id = id;
        Capability = capability;
        Scope = scope;
        GrantedBy = grantedBy;
        GrantedAt = grantedAt;
        SourcePreset = sourcePreset;
    }

    public Guid Id { get; }
    public string Capability { get; }
    public MandateScope Scope { get; }
    public MemberId GrantedBy { get; }
    public DateTime GrantedAt { get; }
    public string? SourcePreset { get; }
}
