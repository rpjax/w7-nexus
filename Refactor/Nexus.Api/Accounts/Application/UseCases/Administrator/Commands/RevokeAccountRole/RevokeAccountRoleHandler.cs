using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountRole;

public sealed record RevokeAccountRoleCommand(string AccountId, string Role);

public sealed class RevokeAccountRoleResult;

public sealed class RevokeAccountRoleHandler : IRevokeAccountRoleUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;

    public RevokeAccountRoleHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
    }

    public async Task<IOperationResult<RevokeAccountRoleResult>> HandleAsync(
        RevokeAccountRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<RevokeAccountRoleResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<RevokeAccountRoleResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<RevokeAccountRoleResult>.Unauthorized(authorization.AuthorizationErrors);

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var removingAdministrator = string.Equals(
            command.Role,
            global::Refactor.Nexus.Api.Authorization.Roles.Administrator,
            StringComparison.OrdinalIgnoreCase);

        if (removingAdministrator
            && string.Equals(requester.AccountId, account.Id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<RevokeAccountRoleResult>.Failure(
                AccountAdministratorGuards.CannotRevokeOwnAdministratorError());
        }

        var lastAdminError = await AccountAdministratorGuards.EnsureNotLastAdministratorAsync(
            account,
            command.Role,
            _accountReadRepository,
            cancellationToken);
        if (lastAdminError is not null)
            return OperationResult<RevokeAccountRoleResult>.Failure(lastAdminError);

        var mutation = account.RemoveRole(command.Role);
        if (mutation.IsFailure)
            return OperationResult<RevokeAccountRoleResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        return OperationResult<RevokeAccountRoleResult>.Success(new RevokeAccountRoleResult());
    }
}
