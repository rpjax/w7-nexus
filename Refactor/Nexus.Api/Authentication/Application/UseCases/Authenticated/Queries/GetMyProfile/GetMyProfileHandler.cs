using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Authenticated.Queries;
using Refactor.Nexus.Api.Authentication.Domain.Errors;

namespace Refactor.Nexus.Api.Authentication.Application.UseCases.Authenticated.Queries.GetMyProfile;

public sealed record GetMyProfileQuery;

public sealed class GetMyProfileResult
{
    public required MyProfileView Profile { get; init; }
}

public sealed class GetMyProfileHandler : IGetMyProfileUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAccountRepository _accountRepository;

    public GetMyProfileHandler(
        IRequestContext requestContext,
        IAccountRepository accountRepository)
    {
        _requestContext = requestContext;
        _accountRepository = accountRepository;
    }

    public async Task<IOperationResult<GetMyProfileResult>> HandleAsync(
        GetMyProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<GetMyProfileResult>.Failure(requesterResult.Errors);

        if (!AccountId.TryParse(requester.AccountId, out var accountId))
        {
            return OperationResult<GetMyProfileResult>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.AccountNotFound)
                .WithMessage("A conta autenticada nao foi encontrada.")
                .Build());
        }

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return OperationResult<GetMyProfileResult>.Failure(Error.Create()
                .WithCode(AuthenticationErrorCodes.AccountNotFound)
                .WithMessage("A conta autenticada nao foi encontrada.")
                .Build());
        }

        return OperationResult<GetMyProfileResult>.Success(new GetMyProfileResult
        {
            Profile = MyProfileView.FromAccount(account)
        });
    }
}
