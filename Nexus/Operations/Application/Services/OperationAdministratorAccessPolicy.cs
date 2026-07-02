using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Application.Services;

public sealed class OperationAdministratorAccessPolicy : IOperationAdministratorAccessPolicy
{
    private IOperationRepository _operations { get; }
    private ITeamRepository _teams { get; }

    public OperationAdministratorAccessPolicy(
        IOperationRepository operations,
        ITeamRepository teams)
    {
        _operations = operations;
        _teams = teams;
    }

    public async Task<IAuthorizationResult> AuthorizeSearchOperationsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var hasAssignedOperation = await _operations.AsQueryable()
            .Where(o => o.AdministratorIds.Contains(identity.AccountId))
            .AnyAsync();

        if (!hasAssignedOperation)
        {
            return AuthorizationResult.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotOperationAdministrator)
                .WithMessage("Acesso de administrador de operação necessário para realizar esta ação.")
                .Build());
        }

        return AuthorizationResult.Authorized();
    }

    public Task<IAuthorizationResult> AuthorizeManageOperationAsync(
        RequesterIdentity identity,
        string? operationId = null,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        if (teamId is not null)
        {
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Failure(Error.Create()
                    .WithCode(TeamErrorCodes.TeamIdInvalid)
                    .WithMessage("O ID da equipe é obrigatório.")
                    .Build()));
            }

            return AuthorizeManageOperationByTeamAsync(identity, teamId.Trim(), cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build()));
        }

        return AuthorizeManageOperationByOperationAsync(identity, operationId.Trim(), cancellationToken);
    }

    private async Task<IAuthorizationResult> AuthorizeManageOperationByTeamAsync(
        RequesterIdentity identity,
        string teamId,
        CancellationToken cancellationToken)
    {
        var team = await _teams.AsQueryable()
            .Where(t => t.Id == teamId)
            .FirstOrDefaultAsync();

        if (team is null)
        {
            return AuthorizationResult.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamNotFound)
                .WithMessage($"A equipe '{teamId}' não foi encontrada.")
                .Build());
        }

        return await AuthorizeManageOperationByOperationAsync(identity, team.OperationId, cancellationToken);
    }

    private async Task<IAuthorizationResult> AuthorizeManageOperationByOperationAsync(
        RequesterIdentity identity,
        string operationId,
        CancellationToken cancellationToken)
    {
        var operation = await _operations.AsQueryable()
            .Where(o => o.Id == operationId)
            .FirstOrDefaultAsync();

        if (operation is null)
        {
            return AuthorizationResult.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());
        }

        if (!operation.AdministratorIds.Contains(identity.AccountId, StringComparer.Ordinal))
        {
            return AuthorizationResult.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotOperationAdministrator)
                .WithMessage("Acesso de administrador de operação necessário para realizar esta ação.")
                .Build());
        }

        return AuthorizationResult.Authorized();
    }
}
