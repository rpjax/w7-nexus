using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountPermission;

public sealed record RevokeAccountPermissionCommand(string AccountId, string Permission);

public sealed class RevokeAccountPermissionResult;

public sealed class RevokeAccountPermissionHandler : IRevokeAccountPermissionUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;

    public RevokeAccountPermissionHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
    }

    public async Task<IOperationResult<RevokeAccountPermissionResult>> HandleAsync(
        RevokeAccountPermissionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevokeAccountPermissionResult>.Failure(RequestBodyRequiredError());

        var access = await AuthorizeAsync(cancellationToken);
        if (access is not null)
            return access;

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<RevokeAccountPermissionResult>.Failure(NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<RevokeAccountPermissionResult>.Failure(NotFoundError(command.AccountId));

        var mutation = account.RemovePermission(command.Permission);
        if (mutation.IsFailure)
            return OperationResult<RevokeAccountPermissionResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        return OperationResult<RevokeAccountPermissionResult>.Success(new RevokeAccountPermissionResult());
    }

    private async Task<IOperationResult<RevokeAccountPermissionResult>?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<RevokeAccountPermissionResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<RevokeAccountPermissionResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<RevokeAccountPermissionResult>.Unauthorized(authorization.AuthorizationErrors);

        return null;
    }

    private static Error RequestBodyRequiredError() =>
        Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    private static Error NotFoundError(string accountId) =>
        Error.Create()
            .WithCode(AccountErrorCodes.AccountNotFound)
            .WithMessage($"A conta '{accountId}' nao foi encontrada.")
            .Build();
}
