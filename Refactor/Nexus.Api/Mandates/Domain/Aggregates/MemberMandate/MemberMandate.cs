using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.Events;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;

public sealed class MemberMandate
{
    private readonly List<MandateGrant> _grants = [];
    private readonly HashSet<string> _appliedPresets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<object> _uncommitted = [];

    public MemberMandate()
    {
    }

    private MemberMandate(
        MemberId memberId,
        IEnumerable<MandateGrant> grants,
        IEnumerable<string> appliedPresets)
    {
        MemberId = memberId;
        _grants.AddRange(grants);
        foreach (var preset in appliedPresets)
            _appliedPresets.Add(preset);
    }

    public MemberId MemberId { get; private set; }
    public Guid PersistenceId => MemberId.Value;
    public IReadOnlyList<MandateGrant> Grants => _grants;
    public IReadOnlyCollection<string> AppliedPresets => _appliedPresets;

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public void ClearUncommitted() => _uncommitted.Clear();

    public static MemberMandate Empty(MemberId memberId)
    {
        var mandate = new MemberMandate();
        mandate.ApplyChange(new MandateOpened(memberId.Value, DateTime.UtcNow, null));
        return mandate;
    }

    public static MemberMandate Rehydrate(
        MemberId memberId,
        IEnumerable<MandateGrant> grants,
        IEnumerable<string> appliedPresets) =>
        new(memberId, grants, appliedPresets);

    public bool HasCapability(string capability, MandateScope requiredScope)
    {
        return _grants.Any(grant =>
            string.Equals(grant.Capability, capability, StringComparison.Ordinal)
            && grant.Scope.Covers(requiredScope));
    }

    public bool CoversGrant(string capability, MandateScope scope) =>
        HasCapability(capability, scope);

    public IResult GrantPreset(
        string presetId,
        MemberId grantedBy,
        bool grantorIsAdministrator,
        MemberMandate? grantorMandate)
    {
        if (!PresetCatalog.TryGetBundle(presetId, out var bundle))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.PresetUnknown)
                .WithMessage($"Preset '{presetId}' desconhecido.")
                .Build());
        }

        var normalized = PresetIds.Normalize(presetId);
        if (_appliedPresets.Contains(normalized))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.PresetAlreadyGranted)
                .WithMessage($"O preset '{normalized}' ja foi concedido a este membro.")
                .Build());
        }

        foreach (var spec in bundle)
        {
            var attenuation = EnsureAttenuation(spec.Capability, spec.Scope, grantorIsAdministrator, grantorMandate);
            if (attenuation.IsFailure)
                return attenuation;
        }

        var now = DateTime.UtcNow;
        var added = new List<MandateGrantDto>();
        foreach (var spec in bundle)
        {
            if (_grants.Any(g =>
                    string.Equals(g.Capability, spec.Capability, StringComparison.Ordinal)
                    && g.Scope.Equals(spec.Scope)))
            {
                continue;
            }

            added.Add(new MandateGrantDto(
                Guid.NewGuid(),
                spec.Capability,
                spec.Scope.ToStorageJson(),
                grantedBy.Value,
                now,
                normalized));
        }

        ApplyChange(new MandatePresetGranted(MemberId.Value, normalized, grantedBy.Value, added.ToArray(), now, grantedBy.Value));
        return Result.Success();
    }

    public IResult RevokePreset(string presetId)
    {
        if (!PresetIds.IsKnown(presetId))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.PresetUnknown)
                .WithMessage($"Preset '{presetId}' desconhecido.")
                .Build());
        }

        var normalized = PresetIds.Normalize(presetId);
        if (!_appliedPresets.Contains(normalized))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.PresetNotGranted)
                .WithMessage($"O preset '{normalized}' nao esta concedido a este membro.")
                .Build());
        }

        ApplyChange(new MandatePresetRevoked(MemberId.Value, normalized, DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult GrantCapability(
        string capability,
        MandateScope scope,
        MemberId grantedBy,
        bool grantorIsAdministrator,
        MemberMandate? grantorMandate)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityEmpty)
                .WithMessage("Capacidade obrigatoria.")
                .Build());
        }

        var trimmed = capability.Trim();
        if (!Capabilities.IsKnown(trimmed))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityUnknown)
                .WithMessage($"Capacidade '{trimmed}' desconhecida.")
                .Build());
        }

        var attenuation = EnsureAttenuation(trimmed, scope, grantorIsAdministrator, grantorMandate);
        if (attenuation.IsFailure)
            return attenuation;

        if (_grants.Any(g =>
                string.Equals(g.Capability, trimmed, StringComparison.Ordinal)
                && g.Scope.Equals(scope)))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.GrantAlreadyExists)
                .WithMessage("Este grant ja existe no mandato do membro.")
                .Build());
        }

        var grant = new MandateGrantDto(
            Guid.NewGuid(),
            trimmed,
            scope.ToStorageJson(),
            grantedBy.Value,
            DateTime.UtcNow,
            null);
        ApplyChange(new MandateCapabilityGranted(MemberId.Value, grant, DateTime.UtcNow, grantedBy.Value));
        return Result.Success();
    }

    public IResult RevokeCapability(string capability, MandateScope scope)
    {
        var trimmed = capability.Trim();
        if (!_grants.Any(g =>
                string.Equals(g.Capability, trimmed, StringComparison.Ordinal)
                && g.Scope.Equals(scope)))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.GrantNotFound)
                .WithMessage("Grant nao encontrado no mandato do membro.")
                .Build());
        }

        ApplyChange(new MandateCapabilityRevoked(MemberId.Value, trimmed, scope.ToStorageJson(), DateTime.UtcNow, null));
        return Result.Success();
    }

    /// <summary>
    /// Removes grants that are no longer covered by the grantor's remaining umbrella.
    /// Returns the number of pruned grants.
    /// </summary>
    public int PruneToUmbrella(MemberMandate grantorMandate, bool grantorIsAdministrator)
    {
        if (grantorIsAdministrator)
            return 0;

        var toRemove = _grants.Where(grant =>
            grant.GrantedBy.Equals(grantorMandate.MemberId)
            && !grantorMandate.CoversGrant(grant.Capability, grant.Scope)).ToList();
        if (toRemove.Count == 0)
            return 0;

        var remaining = _grants.Except(toRemove).ToList();
        var remainingSources = remaining
            .Select(g => g.SourcePreset)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        var remainingPresets = _appliedPresets.Where(remainingSources.Contains).ToArray();

        ApplyChange(new MandateGrantsPruned(
            MemberId.Value,
            toRemove.Select(g => g.Id).ToArray(),
            remainingPresets,
            DateTime.UtcNow,
            null));
        return toRemove.Count;
    }

    /// <summary>
    /// Removes every grant that was issued by the given grantor (full cascade drop).
    /// </summary>
    public int RemoveGrantsIssuedBy(MemberId grantorId) =>
        _grants.RemoveAll(grant => grant.GrantedBy.Equals(grantorId));

    private static IResult EnsureAttenuation(
        string capability,
        MandateScope scope,
        bool grantorIsAdministrator,
        MemberMandate? grantorMandate)
    {
        if (grantorIsAdministrator)
            return Result.Success();

        if (grantorMandate is null || !grantorMandate.CoversGrant(capability, scope))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.AttenuationViolated)
                .WithMessage("O concedente nao possui capacidade/escopo suficiente (atenuacao).")
                .Build());
        }

        return Result.Success();
    }

    public void Apply(MandateOpened e) => MemberId = new MemberId(e.MemberId);

    public void Apply(MandateBackfilled e)
    {
        MemberId = new MemberId(e.MemberId);
        _grants.Clear();
        foreach (var dto in e.Grants)
            _grants.Add(FromDto(dto));
        _appliedPresets.Clear();
        foreach (var preset in e.Presets)
            _appliedPresets.Add(preset);
    }

    public void Apply(MandatePresetGranted e)
    {
        MemberId = new MemberId(e.MemberId);
        _appliedPresets.Add(e.PresetId);
        foreach (var dto in e.AddedGrants)
            _grants.Add(FromDto(dto));
    }

    public void Apply(MandatePresetRevoked e)
    {
        _appliedPresets.Remove(e.PresetId);
        _grants.RemoveAll(g =>
            string.Equals(g.SourcePreset, e.PresetId, StringComparison.OrdinalIgnoreCase));
    }

    public void Apply(MandateCapabilityGranted e)
    {
        MemberId = new MemberId(e.MemberId);
        _grants.Add(FromDto(e.Grant));
    }

    public void Apply(MandateCapabilityRevoked e)
    {
        var scope = MandateScope.FromStorageJson(e.ScopeJson);
        _grants.RemoveAll(g =>
            string.Equals(g.Capability, e.Capability, StringComparison.Ordinal)
            && g.Scope.Equals(scope));
    }

    public void Apply(MandateGrantsPruned e)
    {
        var removed = e.RemovedGrantIds.ToHashSet();
        _grants.RemoveAll(g => removed.Contains(g.Id));
        _appliedPresets.Clear();
        foreach (var preset in e.RemainingPresets)
            _appliedPresets.Add(preset);
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case MandateOpened e: Apply(e); break;
            case MandateBackfilled e: Apply(e); break;
            case MandatePresetGranted e: Apply(e); break;
            case MandatePresetRevoked e: Apply(e); break;
            case MandateCapabilityGranted e: Apply(e); break;
            case MandateCapabilityRevoked e: Apply(e); break;
            case MandateGrantsPruned e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }

        _uncommitted.Add(@event);
    }

    private static MandateGrant FromDto(MandateGrantDto dto) =>
        new(
            dto.Id,
            dto.Capability,
            MandateScope.FromStorageJson(dto.ScopeJson),
            new MemberId(dto.GrantedBy),
            dto.GrantedAt,
            dto.SourcePreset);
}
