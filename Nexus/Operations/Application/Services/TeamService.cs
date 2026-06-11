using Aidan.Core.Errors;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Operations.Application.Services.Contracts;
using Aidan.Core.Patterns;
using Nexus.Actors.Responses.Models;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Accounts.Application.Services.Contracts;
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

    public async Task<IResult<TeamDetails>> CreateTeamAsync(string operationId, string? name)
    {
        var builder = Result.Create<TeamDetails>();

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
                .WithMessage($"Operation '{normalizedOperationId}' was not found")
                .Build());
            return builder.Build();
        }

        name = name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.NameInvalid)
                .WithMessage("Team name is required")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > Team.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(TeamErrorCodes.NameTooLong)
                .WithMessage($"Team name must be at most {Team.MaxNameLength} characters")
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
                .WithMessage($"Team name '{name}' is already in use for this operation")
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
            GatewaySelectionStrategy: (int)GatewaySelectionStrategy.PerStrawman,
            GatewayCredentialsIds: Array.Empty<string>(),
            GatewayCredentialsGroupIds: Array.Empty<string>(),
            OperatorProfitShareRules: Array.Empty<OperatorProfitShareRuleRecord>(),
            CreatedAt: now,
            UpdatedAt: now);

        team = await _teams.CreateAsync(team);

        return builder
            .WithValue(TeamDetails.FromTeam(team))
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
            "Team leader");
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
            "Operator");
        if (accountValidation is not null)
            return accountValidation;

        var normalizedOperatorId = operatorId!.Trim();
        var alreadyAssignedElsewhere = _teams.AsQueryable()
            .Any(t => t.Id != normalizedTeamId && t.OperatorIds.Contains(normalizedOperatorId));
        if (alreadyAssignedElsewhere)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.OperatorAlreadyAssignedToAnotherTeam)
                .WithMessage($"Operator '{normalizedOperatorId}' is already assigned to another team")
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
            "Straw man");
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
                .WithMessage("Gateway selection strategy is invalid")
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
                .WithMessage("Gateway credentials group ID is required")
                .Build());
        }

        var normalizedGroupId = groupId.Trim();
        var groupExists = _gatewayCredentialsGroups.AsQueryable()
            .Any(g => g.Id == normalizedGroupId);
        if (!groupExists)
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialsGroupNotFound)
                .WithMessage($"Gateway credentials group '{normalizedGroupId}' was not found")
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
                .WithMessage("Gateway credentials group ID is required")
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
                .WithMessage("Gateway credential ID is required")
                .Build());
        }

        var normalizedCredentialsId = credentialsId.Trim();
        if (!await _gatewayCredentialsIdValidator.ExistsAsync(normalizedCredentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(TeamErrorCodes.GatewayCredentialInvalid)
                .WithMessage($"Gateway credential '{normalizedCredentialsId}' was not found")
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
                    .WithMessage("Profit share cut account ID cannot be empty")
                    .Build());
            }

            if (!await _accountIdValidator.ExistsAsync(cut.AccountId))
            {
                return Result.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.ProfitShareCutAccountNotFound)
                    .WithMessage($"Profit share cut account '{cut.AccountId}' was not found")
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
                .WithMessage($"{label} ID cannot be empty")
                .Build());
        }

        var normalizedAccountId = accountId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedAccountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"{label} account '{normalizedAccountId}' was not found")
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
                .WithMessage("Operator ID is required")
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
                .WithMessage("Team ID is required")
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
                .WithMessage("Operation ID is required")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string teamId)
    {
        return Result.Failure(Error.Create()
            .WithCode(TeamErrorCodes.TeamNotFound)
            .WithMessage($"Team '{teamId}' was not found")
            .Build());
    }
}
