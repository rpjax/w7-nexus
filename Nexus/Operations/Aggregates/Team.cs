using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Database.Models;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Aggregates;

public sealed class Team
{
    public const int MaxNameLength = 200;

    private readonly List<string> _operatorIds;
    private readonly List<string> _strawManIds;
    private readonly List<string> _gatewayCredentialsGroupIds;
    private readonly List<string> _gatewayCredentialsIds;
    private readonly Dictionary<string, ProfitShareRule> _operatorProfitShareRules;

    public string Id { get; }
    public string OperationId { get; }
    public string Name { get; }
    public string? TeamLeaderId { get; private set; }
    public GatewaySelectionStrategy GatewaySelectionStrategy { get; private set; }
    public IReadOnlyList<string> OperatorIds => _operatorIds.AsReadOnly();
    public IReadOnlyList<string> StrawManIds => _strawManIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsGroupIds => _gatewayCredentialsGroupIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsIds => _gatewayCredentialsIds.AsReadOnly();
    public IReadOnlyDictionary<string, ProfitShareRule> OperatorProfitShareRules
        => _operatorProfitShareRules;
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public bool ManuallySetGatewayCredentials => GatewaySelectionStrategy == GatewaySelectionStrategy.Manual;

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal Team(
        string Id,
        string OperationId,
        string Name,
        string? TeamLeaderId,
        IReadOnlyList<string> OperatorIds,
        IReadOnlyList<string> StrawManIds,
        int GatewaySelectionStrategy,
        IReadOnlyList<string> GatewayCredentialsIds,
        IReadOnlyList<string> GatewayCredentialsGroupIds,
        IReadOnlyList<OperatorProfitShareRuleRecord> OperatorProfitShareRules,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id.Trim();
        this.OperationId = OperationId.Trim();
        this.Name = Name.Trim();
        TeamLeaderId = string.IsNullOrWhiteSpace(TeamLeaderId) ? null : TeamLeaderId.Trim();
        _operatorIds = NormalizeIds(OperatorIds);
        _strawManIds = NormalizeIds(StrawManIds);
        this.GatewaySelectionStrategy = ParseGatewaySelectionStrategy(GatewaySelectionStrategy);
        _gatewayCredentialsGroupIds = NormalizeIds(GatewayCredentialsGroupIds);
        _gatewayCredentialsIds = NormalizeIds(GatewayCredentialsIds);
        _operatorProfitShareRules = NormalizeProfitShareRules(OperatorProfitShareRules);
        EnsureOperatorProfitShareInvariant();
        CreatedAt = CreatedAt;
        UpdatedAt = UpdatedAt;
    }

    public IResult AssignTeamLeader(string teamLeaderId)
    {
        if (string.IsNullOrWhiteSpace(teamLeaderId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderInvalid)
                .WithMessage("Team leader ID cannot be empty")
                .Build());

        var normalizedTeamLeaderId = teamLeaderId.Trim();

        if (TeamLeaderId is not null &&
            string.Equals(TeamLeaderId, normalizedTeamLeaderId, StringComparison.Ordinal))
            return Result.Success();

        if (TeamLeaderId is not null)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderAlreadyAssigned)
                .WithMessage($"Team leader '{TeamLeaderId}' is already assigned to this team")
                .Build());

        TeamLeaderId = normalizedTeamLeaderId;
        Touch();

        return Result.Success();
    }

    public IResult UnassignTeamLeader()
    {
        if (TeamLeaderId is null)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderNotAssigned)
                .WithMessage("This team does not have a team leader assigned")
                .Build());

        TeamLeaderId = null;
        Touch();

        return Result.Success();
    }

    public IResult AssignOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (_operatorIds.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorAlreadyAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is already assigned to this team")
                .Build());

        _operatorIds.Add(normalizedOperatorId);
        _operatorProfitShareRules[normalizedOperatorId] = CreateDefaultProfitShareRule(normalizedOperatorId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();
        var removed = _operatorIds.Remove(normalizedOperatorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorNotAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is not assigned to this team")
                .Build());

        _operatorProfitShareRules.Remove(normalizedOperatorId);
        Touch();

        return Result.Success();
    }

    public IResult SetGatewaySelectionStrategy(GatewaySelectionStrategy strategy)
    {
        if (!Enum.IsDefined(strategy))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewaySelectionStrategyInvalid)
                .WithMessage("Gateway selection strategy is invalid")
                .Build());

        if (GatewaySelectionStrategy == strategy)
            return Result.Success();

        GatewaySelectionStrategy = strategy;
        Touch();

        return Result.Success();
    }

    public IResult SetOperatorProfitShareRule(string operatorId, IReadOnlyList<ProfitSplit> cuts)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (!_operatorIds.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorNotAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is not assigned to this team")
                .Build());

        var validation = ValidateProfitShareCuts(cuts, out var normalizedCuts);
        if (validation is not null)
            return validation;

        if (_operatorProfitShareRules.TryGetValue(normalizedOperatorId, out var current) &&
            RulesAreEqual(current, normalizedCuts))
            return Result.Success();

        _operatorProfitShareRules[normalizedOperatorId] = new ProfitShareRule(
            normalizedOperatorId,
            normalizedCuts);
        Touch();

        return Result.Success();
    }

    public IResult AssignStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID cannot be empty")
                .Build());

        var normalizedStrawManId = strawManId.Trim();

        if (_strawManIds.Contains(normalizedStrawManId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManAlreadyAssigned)
                .WithMessage($"Straw man '{normalizedStrawManId}' is already assigned to this team")
                .Build());

        _strawManIds.Add(normalizedStrawManId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID cannot be empty")
                .Build());

        var normalizedStrawManId = strawManId.Trim();
        var removed = _strawManIds.Remove(normalizedStrawManId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManNotAssigned)
                .WithMessage($"Straw man '{normalizedStrawManId}' is not assigned to this team")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Gateway credential groups can only be assigned when the selection strategy is PerGroup")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID cannot be empty")
                .Build());

        var normalizedGroupId = groupId.Trim();

        if (_gatewayCredentialsGroupIds.Contains(normalizedGroupId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupAlreadyAssigned)
                .WithMessage($"Gateway credentials group '{normalizedGroupId}' is already assigned to this team")
                .Build());

        _gatewayCredentialsGroupIds.Add(normalizedGroupId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Gateway credential groups can only be unassigned when the selection strategy is PerGroup")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID cannot be empty")
                .Build());

        var normalizedGroupId = groupId.Trim();
        var removed = _gatewayCredentialsGroupIds.Remove(normalizedGroupId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupNotAssigned)
                .WithMessage($"Gateway credentials group '{normalizedGroupId}' is not assigned to this team")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ManualGatewayCredentialsDisabled)
                .WithMessage("Manual gateway credential selection is not enabled for this team")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();

        if (_gatewayCredentialsIds.Contains(normalized, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialAlreadyAssigned)
                .WithMessage($"Gateway credential '{normalized}' is already assigned to this team")
                .Build());

        _gatewayCredentialsIds.Add(normalized);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ManualGatewayCredentialsDisabled)
                .WithMessage("Manual gateway credential selection is not enabled for this team")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();
        var removed = _gatewayCredentialsIds.Remove(normalized);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialNotAssigned)
                .WithMessage($"Gateway credential '{normalized}' is not assigned to this team")
                .Build());

        Touch();

        return Result.Success();
    }

    private static IResult? ValidateProfitShareCuts(
        IReadOnlyList<ProfitSplit>? cuts,
        out Dictionary<string, ProfitSplit> normalizedCuts)
    {
        normalizedCuts = new Dictionary<string, ProfitSplit>(StringComparer.Ordinal);

        if (cuts is null || cuts.Count == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ProfitShareRuleEmpty)
                .WithMessage("Profit share rule must contain at least one cut")
                .Build());
        }

        decimal totalPercentage = 0m;

        foreach (var cut in cuts)
        {
            if (string.IsNullOrWhiteSpace(cut.AccountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutAccountInvalid)
                    .WithMessage("Profit share cut account ID cannot be empty")
                    .Build());
            }

            var accountId = cut.AccountId.Trim();

            if (normalizedCuts.ContainsKey(accountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutDuplicateAccount)
                    .WithMessage($"Account '{accountId}' appears more than once in the profit share rule")
                    .Build());
            }

            if (cut.Percentage <= 0m || cut.Percentage > 100m)
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutPercentageInvalid)
                    .WithMessage($"Profit share percentage for account '{accountId}' must be greater than zero and at most 100")
                    .Build());
            }

            normalizedCuts[accountId] = new ProfitSplit(accountId, cut.Percentage);
            totalPercentage += cut.Percentage;
        }

        if (totalPercentage != 100m)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ProfitShareCutsMustTotal100Percent)
                .WithMessage("Profit share cuts must total exactly 100%")
                .Build());
        }

        return null;
    }

    private static bool RulesAreEqual(ProfitShareRule current, Dictionary<string, ProfitSplit> nextCuts)
    {
        if (current.ProfitSplits.Count != nextCuts.Count)
            return false;

        foreach (var entry in nextCuts)
        {
            if (!current.ProfitSplits.TryGetValue(entry.Key, out var existing))
                return false;

            if (existing.Percentage != entry.Value.Percentage)
                return false;
        }

        return true;
    }

    private static GatewaySelectionStrategy ParseGatewaySelectionStrategy(int strategy)
        => Enum.IsDefined(typeof(GatewaySelectionStrategy), strategy)
            ? (GatewaySelectionStrategy)strategy
            : GatewaySelectionStrategy.PerStrawman;

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids)
        => (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static Dictionary<string, ProfitShareRule> NormalizeProfitShareRules(
        IReadOnlyList<OperatorProfitShareRuleRecord>? records)
    {
        var result = new Dictionary<string, ProfitShareRule>(StringComparer.Ordinal);

        foreach (var record in records ?? Array.Empty<OperatorProfitShareRuleRecord>())
        {
            if (string.IsNullOrWhiteSpace(record.OperatorId))
                continue;

            var rule = ProfitShareRule.FromRecord(record);
            result[rule.OperatorId] = rule;
        }

        return result;
    }

    private void EnsureOperatorProfitShareInvariant()
    {
        var orphanOperatorIds = _operatorProfitShareRules.Keys
            .Where(id => !_operatorIds.Contains(id, StringComparer.Ordinal))
            .ToList();

        foreach (var orphanOperatorId in orphanOperatorIds)
            _operatorProfitShareRules.Remove(orphanOperatorId);

        foreach (var operatorId in _operatorIds)
        {
            if (!_operatorProfitShareRules.TryGetValue(operatorId, out var rule) ||
                !IsValidProfitShareRule(rule))
            {
                _operatorProfitShareRules[operatorId] = CreateDefaultProfitShareRule(operatorId);
            }
        }
    }

    private static ProfitShareRule CreateDefaultProfitShareRule(string operatorId)
    {
        var normalizedOperatorId = operatorId.Trim();
        var cuts = new Dictionary<string, ProfitSplit>(StringComparer.Ordinal)
        {
            [normalizedOperatorId] = new ProfitSplit(normalizedOperatorId, 100m)
        };

        return new ProfitShareRule(normalizedOperatorId, cuts);
    }

    private static bool IsValidProfitShareRule(ProfitShareRule rule)
    {
        if (rule.ProfitSplits.Count == 0)
            return false;

        decimal totalPercentage = 0m;

        foreach (var cut in rule.ProfitSplits.Values)
        {
            if (string.IsNullOrWhiteSpace(cut.AccountId))
                return false;

            if (cut.Percentage <= 0m || cut.Percentage > 100m)
                return false;

            totalPercentage += cut.Percentage;
        }

        return totalPercentage == 100m;
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
