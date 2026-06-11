using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.ErrorCodes;
using Nexus.Actors.Contracts;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Results;
using Nexus.Authorization.Services.Contracts;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Authorization.Services;

public sealed class OperationAdministratorAccess : IOperationAdministratorAccess
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }
    private IOperationAdministrator _operationAdministrator { get; }

    public OperationAdministratorAccess(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts,
        IOperationRepository operations,
        ITeamRepository teams,
        IOperationAdministrator operationAdministrator)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
        _operations = operations;
        _teams = teams;
        _operationAdministrator = operationAdministrator;
    }

    public Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Task.FromResult<IAccessEvaluationResult<IOperationAdministrator>>(
                AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                    .WithCode(OperationErrorCodes.OperationIdInvalid)
                    .WithMessage("Operation ID is required.")
                    .Build()));
        }

        return ResolveForOperationInternalAsync(operationId.Trim(), cancellationToken);
    }

    public async Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamIdInvalid)
                .WithMessage("Team ID is required.")
                .Build());
        }

        var team = await _teams.AsQueryable()
            .Where(t => t.Id == teamId.Trim())
            .FirstOrDefaultAsync();

        if (team is null)
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamNotFound)
                .WithMessage($"Team '{teamId.Trim()}' was not found.")
                .Build());
        }

        return await ResolveForOperationInternalAsync(team.OperationId, cancellationToken);
    }

    private async Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForOperationInternalAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("An authenticated identity is required.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("Account identity claim is missing.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"Account '{accountId}' was not found.")
                .Build());
        }

        var operation = await _operations.AsQueryable()
            .Where(o => o.Id == operationId)
            .FirstOrDefaultAsync();

        if (operation is null)
        {
            return AccessEvaluationResult<IOperationAdministrator>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperationNotFound)
                .WithMessage($"Operation '{operationId}' was not found.")
                .Build());
        }

        if (!operation.AdministratorIds.Contains(accountId, StringComparer.Ordinal))
        {
            return AccessEvaluationResult<IOperationAdministrator>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotOperationAdministrator)
                .WithMessage("Operation administrator access is required.")
                .Build());
        }

        return AccessEvaluationResult<IOperationAdministrator>.Authorized(_operationAdministrator);
    }
}
