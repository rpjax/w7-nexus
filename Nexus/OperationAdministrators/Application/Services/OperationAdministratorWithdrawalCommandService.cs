using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.Withdrawals.Application.Contracts;

namespace Nexus.OperationAdministrators.Application.Services;

public sealed class OperationAdministratorWithdrawalCommandService : IOperationAdministratorWithdrawalCommandService
{
    private readonly IOperationAdministratorAccessPolicy _policy;
    private readonly IWithdrawalService _withdrawals;

    public OperationAdministratorWithdrawalCommandService(
        IOperationAdministratorAccessPolicy policy,
        IWithdrawalService withdrawals)
    {
        _policy = policy;
        _withdrawals = withdrawals;
    }

    public async Task<IOperationResult<Withdrawals.Aggregates.Withdrawal>> CreateWithdrawalAsync(
        RequesterIdentity identity,
        CreateWithdrawalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return OperationResult<Withdrawals.Aggregates.Withdrawal>.Failure(Error.Create()
                .WithCode("Withdrawal.REQUEST_BODY_REQUIRED")
                .WithMessage("O corpo da requisição é obrigatório.")
                .Build());
        }

        var authorization = await _policy.AuthorizeManageOperationAsync(
            identity,
            request.OperationId,
            cancellationToken: cancellationToken);

        if (!authorization.IsAuthorized)
            return OperationResult<Withdrawals.Aggregates.Withdrawal>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _withdrawals.CreateWithdrawalAsync(request);
        if (result.IsFailure)
            return OperationResult<Withdrawals.Aggregates.Withdrawal>.Failure(result.Errors);

        return OperationResult<Withdrawals.Aggregates.Withdrawal>.Success(result.Value!);
    }

    public async Task<IOperationResult<Withdrawals.Aggregates.Withdrawal>> GetWithdrawalAsync(
        RequesterIdentity identity,
        string withdrawalId,
        CancellationToken cancellationToken = default)
    {
        var result = await _withdrawals.GetByIdAsync(withdrawalId);
        if (result.IsFailure)
            return OperationResult<Withdrawals.Aggregates.Withdrawal>.Failure(result.Errors);

        var authorization = await _policy.AuthorizeManageOperationAsync(
            identity,
            result.Value!.OperationId,
            cancellationToken: cancellationToken);

        if (!authorization.IsAuthorized)
            return OperationResult<Withdrawals.Aggregates.Withdrawal>.Unauthorized(authorization.AuthorizationErrors);

        return OperationResult<Withdrawals.Aggregates.Withdrawal>.Success(result.Value);
    }
}
