using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.GetAccountById;

public sealed record GetAccountByIdQuery(string AccountId);

public sealed class GetAccountByIdResult
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class GetAccountByIdHandler : IGetAccountByIdUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;

    public GetAccountByIdHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
    }

    public async Task<IOperationResult<GetAccountByIdResult>> HandleAsync(
        GetAccountByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query is null || string.IsNullOrWhiteSpace(query.AccountId))
            return OperationResult<GetAccountByIdResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var access = await AuthorizeAsync(cancellationToken);
        if (access is not null)
            return access;

        if (!AccountId.TryParse(query.AccountId, out var accountId))
            return OperationResult<GetAccountByIdResult>.Failure(AccountAdministratorGuards.NotFoundError(query.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<GetAccountByIdResult>.Failure(AccountAdministratorGuards.NotFoundError(query.AccountId));

        return OperationResult<GetAccountByIdResult>.Success(new GetAccountByIdResult
        {
            Account = AccountDetailsView.FromAccount(account)
        });
    }

    private async Task<IOperationResult<GetAccountByIdResult>?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<GetAccountByIdResult>.Failure(requesterResult.Errors);

        if (requester.HasPermission(Permissions.AccountsRead))
            return null;

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (!authorization.IsAuthorized)
            return OperationResult<GetAccountByIdResult>.Unauthorized(
                authorization.AuthorizationErrors.Count > 0
                    ? authorization.AuthorizationErrors
                    : authorization.Errors);
        if (authorization.IsFailure)
            return OperationResult<GetAccountByIdResult>.Failure(authorization.Errors);

        return null;
    }
}
