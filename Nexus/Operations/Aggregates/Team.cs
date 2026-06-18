using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Database.Models;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Aggregates;

public sealed class Team : IGatewayCredentialScope
{
    public const int MaxNameLength = 200;

    private readonly List<string> _operatorIds;
    private readonly List<string> _strawManIds;
    private readonly List<string> _gatewayCredentialsGroupIds;
    private readonly List<string> _gatewayCredentialsIds;
    private readonly Dictionary<string, OperatorProfitShareRuleRecord> _operatorProfitShareRules;

    public string Id { get; }
    public string OperationId { get; }
    public string Name { get; }
    public string? TeamLeaderId { get; private set; }
    public GatewaySelectionStrategy GatewaySelectionStrategy { get; private set; }
    public IReadOnlyList<string> OperatorIds => _operatorIds.AsReadOnly();
    public IReadOnlyList<string> StrawManIds => _strawManIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsGroupIds => _gatewayCredentialsGroupIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsIds => _gatewayCredentialsIds.AsReadOnly();
    public IReadOnlyList<OperatorProfitShareRuleRecord> OperatorProfitShareRules
        => _operatorProfitShareRules.Values.ToList().AsReadOnly();
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
        GatewaySelectionStrategy GatewaySelectionStrategy,
        IReadOnlyList<string> GatewayCredentialsIds,
        IReadOnlyList<string> GatewayCredentialsGroupIds,
        IReadOnlyList<OperatorProfitShareRuleRecord> OperatorProfitShareRules,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id.Trim();
        this.OperationId = OperationId.Trim();
        this.Name = Name.Trim();
        this.TeamLeaderId = string.IsNullOrWhiteSpace(TeamLeaderId) ? null : TeamLeaderId.Trim();
        _operatorIds = NormalizeIds(OperatorIds);
        _strawManIds = NormalizeIds(StrawManIds);
        this.GatewaySelectionStrategy = GatewaySelectionStrategy;
        _gatewayCredentialsGroupIds = NormalizeIds(GatewayCredentialsGroupIds);
        _gatewayCredentialsIds = NormalizeIds(GatewayCredentialsIds);
        _operatorProfitShareRules = NormalizeProfitShareRules(OperatorProfitShareRules);
        EnsureOperatorProfitShareInvariant();
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public IResult AssignTeamLeader(string teamLeaderId)
    {
        if (string.IsNullOrWhiteSpace(teamLeaderId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderInvalid)
                .WithMessage("O ID do líder de equipe não pode estar vazio.")
                .Build());

        var normalizedTeamLeaderId = teamLeaderId.Trim();

        if (TeamLeaderId is not null &&
            string.Equals(TeamLeaderId, normalizedTeamLeaderId, StringComparison.Ordinal))
            return Result.Success();

        if (TeamLeaderId is not null)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamLeaderAlreadyAssigned)
                .WithMessage($"O líder de equipe '{TeamLeaderId}' já está atribuído a esta equipe.")
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
                .WithMessage("Esta equipe não possui um líder de equipe atribuído.")
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
                .WithMessage("O ID do operador não pode estar vazio.")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (_operatorIds.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorAlreadyAssigned)
                .WithMessage($"O operador '{normalizedOperatorId}' já está atribuído a esta equipe.")
                .Build());

        _operatorIds.Add(normalizedOperatorId);
        _operatorProfitShareRules[normalizedOperatorId] = CreateDefaultRule(normalizedOperatorId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorInvalid)
                .WithMessage("O ID do operador não pode estar vazio.")
                .Build());

        var normalizedOperatorId = operatorId.Trim();
        var removed = _operatorIds.Remove(normalizedOperatorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorNotAssigned)
                .WithMessage($"O operador '{normalizedOperatorId}' não está atribuído a esta equipe.")
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
                .WithMessage("A estratégia de seleção de gateway é inválida.")
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
                .WithMessage("O ID do operador não pode estar vazio.")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (!_operatorIds.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorNotAssigned)
                .WithMessage($"O operador '{normalizedOperatorId}' não está atribuído a esta equipe.")
                .Build());

        var validation = ValidateProfitShareCuts(cuts, out var normalizedCuts);
        if (validation is not null)
            return validation;

        if (_operatorProfitShareRules.TryGetValue(normalizedOperatorId, out var current) &&
            RulesAreEqual(current, normalizedCuts))
            return Result.Success();

        _operatorProfitShareRules[normalizedOperatorId] = new OperatorProfitShareRuleRecord
        {
            OperatorId = normalizedOperatorId,
            Cuts = normalizedCuts
        };
        Touch();

        return Result.Success();
    }

    public IResult AssignStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja não pode estar vazio.")
                .Build());

        var normalizedStrawManId = strawManId.Trim();

        if (_strawManIds.Contains(normalizedStrawManId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManAlreadyAssigned)
                .WithMessage($"O laranja '{normalizedStrawManId}' já está atribuído a esta equipe.")
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
                .WithMessage("O ID do laranja não pode estar vazio.")
                .Build());

        var normalizedStrawManId = strawManId.Trim();
        var removed = _strawManIds.Remove(normalizedStrawManId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.StrawManNotAssigned)
                .WithMessage($"O laranja '{normalizedStrawManId}' não está atribuído a esta equipe.")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Grupos de credenciais só podem ser atribuídos quando a estratégia de seleção é Por Grupo.")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais não pode estar vazio.")
                .Build());

        var normalizedGroupId = groupId.Trim();

        if (_gatewayCredentialsGroupIds.Contains(normalizedGroupId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupAlreadyAssigned)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' já está atribuído a esta equipe.")
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
                .WithMessage("Grupos de credenciais só podem ser removidos quando a estratégia de seleção é Por Grupo.")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais não pode estar vazio.")
                .Build());

        var normalizedGroupId = groupId.Trim();
        var removed = _gatewayCredentialsGroupIds.Remove(normalizedGroupId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupNotAssigned)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' não está atribuído a esta equipe.")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ManualGatewayCredentialsDisabled)
                .WithMessage("A seleção manual de credenciais de gateway não está habilitada para esta equipe.")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway não pode estar vazio.")
                .Build());

        var normalized = credentialsId.Trim();

        if (_gatewayCredentialsIds.Contains(normalized, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialAlreadyAssigned)
                .WithMessage($"A credencial de gateway '{normalized}' já está atribuída a esta equipe.")
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
                .WithMessage("A seleção manual de credenciais de gateway não está habilitada para esta equipe.")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway não pode estar vazio.")
                .Build());

        var normalized = credentialsId.Trim();
        var removed = _gatewayCredentialsIds.Remove(normalized);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialNotAssigned)
                .WithMessage($"A credencial de gateway '{normalized}' não está atribuída a esta equipe.")
                .Build());

        Touch();

        return Result.Success();
    }

    private static IResult? ValidateProfitShareCuts(
        IReadOnlyList<ProfitSplit>? cuts,
        out List<ProfitSplitRecord> normalizedCuts)
    {
        normalizedCuts = new List<ProfitSplitRecord>();

        if (cuts is null || cuts.Count == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ProfitShareRuleEmpty)
                .WithMessage("A regra de divisão de lucro deve conter pelo menos uma fatia.")
                .Build());
        }

        decimal totalPercentage = 0m;
        var seenAccounts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var cut in cuts)
        {
            if (string.IsNullOrWhiteSpace(cut.AccountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutAccountInvalid)
                    .WithMessage("O ID da conta na divisão de lucro não pode estar vazio.")
                    .Build());
            }

            var accountId = cut.AccountId.Trim();

            if (!seenAccounts.Add(accountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutDuplicateAccount)
                    .WithMessage($"A conta '{accountId}' aparece mais de uma vez na regra de divisão de lucro.")
                    .Build());
            }

            if (cut.Percentage <= 0m || cut.Percentage > 100m)
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutPercentageInvalid)
                    .WithMessage($"A porcentagem de divisão de lucro da conta '{accountId}' deve ser maior que zero e no máximo 100%.")
                    .Build());
            }

            normalizedCuts.Add(new ProfitSplitRecord { AccountId = accountId, Percentage = cut.Percentage });
            totalPercentage += cut.Percentage;
        }

        if (totalPercentage != 100m)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.ProfitShareCutsMustTotal100Percent)
                .WithMessage("As fatias da divisão de lucro devem totalizar exatamente 100%.")
                .Build());
        }

        return null;
    }

    private static bool RulesAreEqual(OperatorProfitShareRuleRecord current, List<ProfitSplitRecord> nextCuts)
    {
        if (current.Cuts.Count != nextCuts.Count)
            return false;

        foreach (var next in nextCuts)
        {
            var existing = current.Cuts.FirstOrDefault(c => c.AccountId == next.AccountId);
            if (existing is null || existing.Percentage != next.Percentage)
                return false;
        }

        return true;
    }

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids)
        => (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static Dictionary<string, OperatorProfitShareRuleRecord> NormalizeProfitShareRules(
        IReadOnlyList<OperatorProfitShareRuleRecord>? records)
    {
        var result = new Dictionary<string, OperatorProfitShareRuleRecord>(StringComparer.Ordinal);

        foreach (var record in records ?? Array.Empty<OperatorProfitShareRuleRecord>())
        {
            if (string.IsNullOrWhiteSpace(record.OperatorId))
                continue;

            var operatorId = record.OperatorId.Trim();
            result[operatorId] = new OperatorProfitShareRuleRecord
            {
                OperatorId = operatorId,
                Cuts = record.Cuts ?? new List<ProfitSplitRecord>()
            };
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
                _operatorProfitShareRules[operatorId] = CreateDefaultRule(operatorId);
            }
        }
    }

    private static OperatorProfitShareRuleRecord CreateDefaultRule(string operatorId)
    {
        var normalizedOperatorId = operatorId.Trim();
        return new OperatorProfitShareRuleRecord
        {
            OperatorId = normalizedOperatorId,
            Cuts = new List<ProfitSplitRecord>
            {
                new ProfitSplitRecord { AccountId = normalizedOperatorId, Percentage = 100m }
            }
        };
    }

    private static bool IsValidProfitShareRule(OperatorProfitShareRuleRecord rule)
    {
        if (rule.Cuts == null || rule.Cuts.Count == 0)
            return false;

        decimal totalPercentage = 0m;

        foreach (var cut in rule.Cuts)
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
