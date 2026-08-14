using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.ResetAccountPassword;

public sealed record ResetAccountPasswordCommand(string AccountId, string NewPassword);

public sealed class ResetAccountPasswordResult
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class ResetAccountPasswordHandler : IResetAccountPasswordUseCase
{
    private const int PasswordMinLength = 8;

    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetAccountPasswordHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IOperationResult<ResetAccountPasswordResult>> HandleAsync(
        ResetAccountPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ResetAccountPasswordResult>.Failure(AccountAdministratorGuards.RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ResetAccountPasswordResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<ResetAccountPasswordResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<ResetAccountPasswordResult>.Unauthorized(authorization.AuthorizationErrors);

        if (string.IsNullOrEmpty(command.NewPassword) || command.NewPassword.Length < PasswordMinLength)
        {
            return OperationResult<ResetAccountPasswordResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordTooShort)
                .WithMessage($"A nova senha deve ter no minimo {PasswordMinLength} caracteres.")
                .Build());
        }

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<ResetAccountPasswordResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<ResetAccountPasswordResult>.Failure(AccountAdministratorGuards.NotFoundError(command.AccountId));

        var newHash = await _passwordHasher.HashAsync(command.NewPassword, cancellationToken);
        var mutation = account.ChangePassword(newHash);
        if (mutation.IsFailure)
            return OperationResult<ResetAccountPasswordResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        return OperationResult<ResetAccountPasswordResult>.Success(new ResetAccountPasswordResult
        {
            Account = AccountDetailsView.FromAccount(account)
        });
    }
}
