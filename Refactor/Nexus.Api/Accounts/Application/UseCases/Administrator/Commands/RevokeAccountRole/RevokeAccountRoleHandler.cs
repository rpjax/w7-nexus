using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountRole;

public sealed record RevokeAccountRoleCommand(string AccountId, string Role);

public sealed class RevokeAccountRoleResult;

public sealed class RevokeAccountRoleHandler : IRevokeAccountRoleUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IJournalWriter _journal;

    public RevokeAccountRoleHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _journal = journal;
    }

    public async Task<IOperationResult<RevokeAccountRoleResult>> HandleAsync(
        RevokeAccountRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var access = await AuthorizeAsync(cancellationToken);
        if (access is not null)
            return access;

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<RevokeAccountRoleResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

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
        _journal.RecordRoleRevoked(account, command.Role.Trim());
        return OperationResult<RevokeAccountRoleResult>.Success(new RevokeAccountRoleResult());
    }

    private async Task<IOperationResult<RevokeAccountRoleResult>?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<RevokeAccountRoleResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<RevokeAccountRoleResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<RevokeAccountRoleResult>.Unauthorized(authorization.AuthorizationErrors);

        return null;
    }
}
