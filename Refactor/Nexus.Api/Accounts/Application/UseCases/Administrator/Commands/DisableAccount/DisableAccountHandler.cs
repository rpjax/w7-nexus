using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.DisableAccount;

public sealed record DisableAccountCommand(string AccountId);

public sealed class DisableAccountResult
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class DisableAccountHandler : IDisableAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IJournalWriter _journal;

    public DisableAccountHandler(
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

    public async Task<IOperationResult<DisableAccountResult>> HandleAsync(
        DisableAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null || string.IsNullOrWhiteSpace(command.AccountId))
            return OperationResult<DisableAccountResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<DisableAccountResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<DisableAccountResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<DisableAccountResult>.Unauthorized(authorization.AuthorizationErrors);

        if (string.Equals(requester.AccountId, command.AccountId.Trim(), StringComparison.OrdinalIgnoreCase))
            return OperationResult<DisableAccountResult>.Failure(AccountAdministratorGuards.CannotDisableSelfError());

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<DisableAccountResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<DisableAccountResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var lastAdminError = await AccountAdministratorGuards.EnsureNotLastAdministratorAsync(
            account,
            roleBeingRemoved: null,
            _accountReadRepository,
            cancellationToken);
        if (lastAdminError is not null)
            return OperationResult<DisableAccountResult>.Failure(lastAdminError);

        var mutation = account.Disable();
        if (mutation.IsFailure)
            return OperationResult<DisableAccountResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        _journal.RecordDisabled(account);

        return OperationResult<DisableAccountResult>.Success(new DisableAccountResult
        {
            Account = AccountDetailsView.FromAccount(account)
        });
    }
}
