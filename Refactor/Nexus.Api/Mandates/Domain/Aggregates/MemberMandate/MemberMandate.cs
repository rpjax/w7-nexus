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
        IEnumerable<string> appliedPresets,
        string attritionStatus,
        string? attritionCause)
    {
        MemberId = memberId;
        _grants.AddRange(grants);
        foreach (var preset in appliedPresets)
            _appliedPresets.Add(preset);
        AttritionStatus = string.IsNullOrWhiteSpace(attritionStatus) ? "active" : attritionStatus;
        AttritionCause = attritionCause;
    }

    public MemberId MemberId { get; private set; }
    public Guid Id => MemberId.Value;
    public Guid PersistenceId => Id;
    public IReadOnlyList<MandateGrant> Grants => _grants;
    public IReadOnlyCollection<string> AppliedPresets => _appliedPresets;
    public string AttritionStatus { get; private set; } = "active";
    public string? AttritionCause { get; private set; }

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
        IEnumerable<string> appliedPresets,
        string attritionStatus = "active",
        string? attritionCause = null) =>
        new(memberId, grants, appliedPresets, attritionStatus, attritionCause);

    public bool IsActive =>
        string.Equals(AttritionStatus, "active", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Unique concedente when every live grant shares one grantor; otherwise the most frequent.
    /// Used as voluntary-exit re-parent target (G4/G11: sobe para o concedente).
    /// </summary>
    public bool TryGetConcedente(out MemberId concedente)
    {
        var ranked = _grants
            .GroupBy(g => g.GrantedBy.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();
        if (ranked.Count == 0)
        {
            concedente = default;
            return false;
        }

        concedente = new MemberId(ranked[0]);
        return true;
    }

    public bool HasCapability(string capability, MandateScope requiredScope)
    {
        if (!IsActive)
            return false;

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

        var inactive = RejectIfInactive();
        if (inactive.IsFailure)
            return inactive;

        var normalized = PresetIds.Normalize(presetId);
        if (_appliedPresets.Contains(normalized))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.PresetAlreadyGranted)
                .WithMessage($"O preset '{normalized}' já foi concedido a este membro.")
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
                .WithMessage($"O preset '{normalized}' não está concedido a este membro.")
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
                .WithMessage("Capacidade obrigatória.")
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

        var inactive = RejectIfInactive();
        if (inactive.IsFailure)
            return inactive;

        var attenuation = EnsureAttenuation(trimmed, scope, grantorIsAdministrator, grantorMandate);
        if (attenuation.IsFailure)
            return attenuation;

        if (_grants.Any(g =>
                string.Equals(g.Capability, trimmed, StringComparison.Ordinal)
                && g.Scope.Equals(scope)))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.GrantAlreadyExists)
                .WithMessage("Este grant já existe no mandato do membro.")
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
                .WithMessage("Grant não encontrado no mandato do membro.")
                .Build());
        }

        ApplyChange(new MandateCapabilityRevoked(MemberId.Value, trimmed, scope.ToStorageJson(), DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult RecordAttrition(string status, string cause)
    {
        var normalizedStatus = (status ?? "").Trim().ToLowerInvariant();
        if (normalizedStatus is not ("burned" or "left" or "betrayed"))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.AttritionInvalid)
                .WithMessage("Estado deve ser queimado, saiu ou traiu.")
                .Build());
        }

        var normalizedCause = (cause ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCause))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.AttritionInvalid)
                .WithMessage("Causa obrigatória.")
                .Build());
        }

        if (!CauseFitsStatus(normalizedStatus, normalizedCause))
        {
            return Result.Failure(Error.Create()
                .WithCode(MandateErrorCodes.AttritionInvalid)
                .WithMessage("Causa incompatível com o estado (queimado não aceita saída voluntária).")
                .Build());
        }

        var alreadyRecorded = string.Equals(AttritionStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase)
            && string.Equals(AttritionCause, normalizedCause, StringComparison.OrdinalIgnoreCase);

        if (!alreadyRecorded)
            ApplyChange(new MemberAttritionRecorded(MemberId.Value, normalizedStatus, normalizedCause, DateTime.UtcNow, null));

        if (normalizedStatus is "burned" or "betrayed")
            StripOwnPower();

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

        ApplyGrantRemoval(toRemove);
        return toRemove.Count;
    }

    /// <summary>
    /// Event-sourced drop of every grant issued by <paramref name="grantorId"/> (queimado/traiu cascade).
    /// </summary>
    public int DropGrantsIssuedBy(MemberId grantorId)
    {
        var toRemove = _grants.Where(grant => grant.GrantedBy.Equals(grantorId)).ToList();
        if (toRemove.Count == 0)
            return 0;

        ApplyGrantRemoval(toRemove);
        return toRemove.Count;
    }

    /// <summary>
    /// Voluntary exit: grants issued by the departing member now belong to the concedente.
    /// </summary>
    public int ReparentGrantsIssuedBy(MemberId fromGrantor, MemberId toGrantor)
    {
        if (fromGrantor.Equals(toGrantor))
            return 0;

        var toMove = _grants.Where(grant => grant.GrantedBy.Equals(fromGrantor)).ToList();
        if (toMove.Count == 0)
            return 0;

        ApplyChange(new MandateGrantsReparented(
            MemberId.Value,
            fromGrantor.Value,
            toGrantor.Value,
            toMove.Select(g => g.Id).ToArray(),
            DateTime.UtcNow,
            null));
        return toMove.Count;
    }

    private IResult RejectIfInactive()
    {
        if (IsActive)
            return Result.Success();

        return Result.Failure(Error.Create()
            .WithCode(MandateErrorCodes.AttritionInvalid)
            .WithMessage("Este membro está queimado, traiu ou saiu; não dá para conceder poder.")
            .Build());
    }

    private static bool CauseFitsStatus(string status, string cause) =>
        status switch
        {
            "burned" => cause is "bloqueio_bancario" or "apreensao" or "erro_operacional" or "estorno" or "desconhecido",
            "betrayed" => cause is "traicao",
            "left" => cause is "saida_voluntaria",
            _ => false
        };

    private void StripOwnPower()
    {
        if (_grants.Count == 0 && _appliedPresets.Count == 0)
            return;

        ApplyGrantRemoval(_grants.ToList());
    }

    private void ApplyGrantRemoval(IReadOnlyList<MandateGrant> toRemove)
    {
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
    }

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
                .WithMessage("O concedente não possui capacidade/escopo suficiente (atenuação).")
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

    public void Apply(MandateGrantsReparented e)
    {
        var moved = e.GrantIds.ToHashSet();
        var toGrantor = new MemberId(e.ToGrantorId);
        for (var i = 0; i < _grants.Count; i++)
        {
            var grant = _grants[i];
            if (!moved.Contains(grant.Id))
                continue;

            _grants[i] = new MandateGrant(
                grant.Id,
                grant.Capability,
                grant.Scope,
                toGrantor,
                grant.GrantedAt,
                grant.SourcePreset);
        }
    }

    public void Apply(MemberAttritionRecorded e)
    {
        MemberId = new MemberId(e.MemberId);
        AttritionStatus = e.Status;
        AttritionCause = e.Cause;
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
            case MandateGrantsReparented e: Apply(e); break;
            case MemberAttritionRecorded e: Apply(e); break;
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
