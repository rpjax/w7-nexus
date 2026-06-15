using Aidan.Core.Errors;
using Nexus.Gateways.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Application.Services;

public sealed class TeamService : ITeamService
{
    private readonly ITeamRepository _teams;
    private readonly IOperationRepository _operations;
    private readonly IAccountIdValidator _accountIdValidator;
    private readonly IGatewayCredentialsGroupRepository _gatewayCredentialsGroups;
    private readonly IGatewayCredentialsIdValidator _gatewayCredentialsIdValidator;

    public TeamService(
        ITeamRepository teams,
        IOperationRepository operations,
        IAccountIdValidator accountIdValidator,
        IGatewayCredentialsGroupRepository gatewayCredentialsGroups,
        IGatewayCredentialsIdValidator gatewayCredentialsIdValidator)
    {
        _teams = teams;
        _operations = operations;
        _accountIdValidator = accountIdValidator;
        _gatewayCredentialsGroups = gatewayCredentialsGroups;
        _gatewayCredentialsIdValidator = gatewayCredentialsIdValidator;
    }

    public async Task<IResult<Team>> CreateTeamAsync(string operationId, string? name)
    {
        var builder = Result.Create<Team>();

        var operationValidation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (operationValidation is not null)
        {
            builder.WithErrors(operationValidation.Errors);
            return builder.Build();
        }

        if (FindOperation(normalizedOperationId) is null)
        {
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{normalizedOperationId}' não foi encontrada.")
                .Build());
            return builder.Build();
        }

        name = name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.NameInvalid)
                .WithMessage("O nome da equipe é obrigatório.")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > Team.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.NameTooLong)
                .WithMessage($"O nome da equipe pode ter no máximo {Team.MaxNameLength} caracteres.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var normalizedName = name!.ToLowerInvariant();
        var nameTaken = _teams.AsQueryable()
            .Any(t => t.OperationId == normalizedOperationId && t.Name.ToLower() == normalizedName);
        if (nameTaken)
        {
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.NameAlreadyExists)
                .WithMessage($"O nome de equipe '{name}' já está em uso nesta operação. Escolha outro nome.")
                .Build());
            return builder.Build();
        }

        var now = DateTime.UtcNow;
        var team = new Team(
            Id: string.Empty,
            OperationId: normalizedOperationId,
            Name: name!,
            TeamLeaderId: null,
            OperatorIds: Array.Empty<string>(),
            StrawManIds: Array.Empty<string>(),
            GatewaySelectionStrategy: GatewaySelectionStrategy.PerStrawman,
            GatewayCredentialsIds: Array.Empty<string>(),
            GatewayCredentialsGroupIds: Array.Empty<string>(),
            OperatorProfitShareRules: Array.Empty<OperatorProfitShareRuleRecord>(),
            CreatedAt: now,
            UpdatedAt: now);

        team = await _teams.CreateAsync(team);

        return builder
            .WithValue(team)
            .Build();
    }

    public async Task<IResult> DeleteTeamAsync(string teamId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        await _teams.DeleteAsync(team);
        return Result.Success();
    }

    public async Task<IResult> AssignTeamLeaderAsync(string teamId, string teamLeaderId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            teamLeaderId,
            TeamErrorCodes.TeamLeaderInvalid,
            TeamErrorCodes.TeamLeaderAccountNotFound,
            "líder de equipe");
        if (accountValidation is not null)
            return accountValidation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.AssignTeamLeader(teamLeaderId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> UnassignTeamLeaderAsync(string teamId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.UnassignTeamLeader();
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> AssignOperatorAsync(string teamId, string operatorId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            operatorId,
            TeamErrorCodes.OperatorInvalid,
            TeamErrorCodes.OperatorAccountNotFound,
            "operador");
        if (accountValidation is not null)
            return accountValidation;

        var normalizedOperatorId = operatorId!.Trim();
        var alreadyAssignedElsewhere = _teams.AsQueryable()
            .Any(t => t.Id != normalizedTeamId && t.OperatorIds.Contains(normalizedOperatorId));
        if (alreadyAssignedElsewhere)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorAlreadyAssignedToAnotherTeam)
                .WithMessage($"O operador '{normalizedOperatorId}' já está atribuído a outra equipe.")
                .Build());
        }

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.AssignOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> UnassignOperatorAsync(string teamId, string operatorId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var operatorValidation = ValidateOperatorId(operatorId, out _);
        if (operatorValidation is not null)
            return operatorValidation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.UnassignOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> AssignStrawManAsync(string teamId, string strawManId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            strawManId,
            TeamErrorCodes.StrawManInvalid,
            TeamErrorCodes.StrawManAccountNotFound,
            "laranja");
        if (accountValidation is not null)
            return accountValidation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.AssignStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> UnassignStrawManAsync(string teamId, string strawManId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.UnassignStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> SetGatewaySelectionStrategyAsync(string teamId, GatewaySelectionStrategy strategy)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        if (!Enum.IsDefined(strategy))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewaySelectionStrategyInvalid)
                .WithMessage("A estratégia de seleção de gateway é inválida.")
                .Build());
        }

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.SetGatewaySelectionStrategy(strategy);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> AssignGatewayCredentialsGroupAsync(string teamId, string groupId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais é obrigatório.")
                .Build());
        }

        var normalizedGroupId = groupId.Trim();
        var groupExists = _gatewayCredentialsGroups.AsQueryable()
            .Any(g => g.Id == normalizedGroupId);
        if (!groupExists)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupNotFound)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' não foi encontrado.")
                .Build());
        }

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.AssignGatewayCredentialsGroup(normalizedGroupId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> UnassignGatewayCredentialsGroupAsync(string teamId, string groupId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais é obrigatório.")
                .Build());
        }

        var normalizedGroupId = groupId.Trim();

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.UnassignGatewayCredentialsGroup(normalizedGroupId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> AssignGatewayCredentialsAsync(string teamId, string credentialsId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway é obrigatório.")
                .Build());
        }

        var normalizedCredentialsId = credentialsId.Trim();
        if (!await _gatewayCredentialsIdValidator.ExistsAsync(normalizedCredentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage($"A credencial de gateway '{normalizedCredentialsId}' não foi encontrada.")
                .Build());
        }

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.AssignGatewayCredentials(normalizedCredentialsId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> UnassignGatewayCredentialsAsync(string teamId, string credentialsId)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var result = team.UnassignGatewayCredentials(credentialsId);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    public async Task<IResult> SetOperatorProfitShareRuleAsync(
        string teamId,
        string operatorId,
        IReadOnlyList<ProfitSplit> cuts)
    {
        var validation = ValidateTeamId(teamId, out var normalizedTeamId);
        if (validation is not null)
            return validation;

        var operatorValidation = ValidateOperatorId(operatorId, out var normalizedOperatorId);
        if (operatorValidation is not null)
            return operatorValidation;

        var team = FindTeam(normalizedTeamId);
        if (team is null)
            return NotFoundResult(normalizedTeamId);

        var normalizedCuts = (cuts ?? Array.Empty<ProfitSplit>())
            .Select(cut => new ProfitSplit(
                string.IsNullOrWhiteSpace(cut.AccountId) ? string.Empty : cut.AccountId.Trim(),
                cut.Percentage))
            .ToList();

        foreach (var cut in normalizedCuts)
        {
            if (string.IsNullOrWhiteSpace(cut.AccountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutAccountInvalid)
                    .WithMessage("O ID da conta na divisão de lucro não pode estar vazio.")
                    .Build());
            }

            if (!await _accountIdValidator.ExistsAsync(cut.AccountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutAccountNotFound)
                    .WithMessage($"A conta '{cut.AccountId}' da divisão de lucro não foi encontrada.")
                    .Build());
            }
        }

        var result = team.SetOperatorProfitShareRule(normalizedOperatorId, normalizedCuts);
        if (result.IsFailure)
            return result;

        await _teams.UpdateAsync(team);
        return Result.Success();
    }

    private Team? FindTeam(string normalizedTeamId)
        => _teams.AsQueryable().FirstOrDefault(t => t.Id == normalizedTeamId);

    private Operation? FindOperation(string normalizedOperationId)
        => _operations.AsQueryable().FirstOrDefault(o => o.Id == normalizedOperationId);

    private async Task<IResult?> ValidateAccountExistsAsync(
        string? accountId,
        string invalidCode,
        string notFoundCode,
        string label)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage($"O ID de {label} não pode estar vazio.")
                .Build());
        }

        var normalizedAccountId = accountId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedAccountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"A conta de {label} '{normalizedAccountId}' não foi encontrada.")
                .Build());
        }

        return null;
    }

    private static IResult? ValidateOperatorId(string? operatorId, out string normalizedOperatorId)
    {
        normalizedOperatorId = operatorId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedOperatorId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());
        }

        return null;
    }

    private static IResult? ValidateTeamId(string? teamId, out string normalizedTeamId)
    {
        normalizedTeamId = teamId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedTeamId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamIdInvalid)
                .WithMessage("O ID da equipe é obrigatório.")
                .Build());
        }

        return null;
    }

    private static IResult? ValidateOperationId(string? operationId, out string normalizedOperationId)
    {
        normalizedOperationId = operationId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedOperationId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string teamId)
    {
        return Result.Failure(Error.Create()
            .WithCode(TeamErrorCodes.TeamNotFound)
            .WithMessage($"A equipe '{teamId}' não foi encontrada.")
            .Build());
    }
}
