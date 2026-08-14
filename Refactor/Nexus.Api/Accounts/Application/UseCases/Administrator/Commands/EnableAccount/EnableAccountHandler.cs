using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.EnableAccount;

public sealed record EnableAccountCommand(string AccountId);

public sealed class EnableAccountResult
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class EnableAccountHandler : IEnableAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;

    public EnableAccountHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
    }

    public async Task<IOperationResult<EnableAccountResult>> HandleAsync(
        EnableAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null || string.IsNullOrWhiteSpace(command.AccountId))
            return OperationResult<EnableAccountResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<EnableAccountResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<EnableAccountResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<EnableAccountResult>.Unauthorized(authorization.AuthorizationErrors);

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<EnableAccountResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<EnableAccountResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var mutation = account.Enable();
        if (mutation.IsFailure)
            return OperationResult<EnableAccountResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        return OperationResult<EnableAccountResult>.Success(new EnableAccountResult
        {
            Account = AccountDetailsView.FromAccount(account)
        });
    }
}
