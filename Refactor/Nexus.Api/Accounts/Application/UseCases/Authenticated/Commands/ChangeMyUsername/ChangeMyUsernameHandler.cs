using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyUsername;

public sealed record ChangeMyUsernameCommand(string NewUsername);

public sealed class ChangeMyUsernameResult
{
    public required string Username { get; init; }
}

public sealed class ChangeMyUsernameHandler : IChangeMyUsernameUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;

    public ChangeMyUsernameHandler(
        IRequestContext requestContext,
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository)
    {
        _requestContext = requestContext;
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
    }

    public async Task<IOperationResult<ChangeMyUsernameResult>> HandleAsync(
        ChangeMyUsernameCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ChangeMyUsernameResult>.Failure(RequestRequired());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ChangeMyUsernameResult>.Failure(requesterResult.Errors);

        if (!AccountId.TryParse(requester.AccountId, out var accountId))
            return OperationResult<ChangeMyUsernameResult>.Failure(AccountNotFound());

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<ChangeMyUsernameResult>.Failure(AccountNotFound());

        if (account.IsDisabled)
        {
            return OperationResult<ChangeMyUsernameResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountDisabled)
                .WithMessage("Esta conta esta desabilitada.")
                .Build());
        }

        var newUsername = command.NewUsername?.Trim() ?? string.Empty;
        var validationErrors = await AccountRegistrationPolicy.ValidateUsernameOnlyAsync(
            newUsername,
            account.Username,
            _accountReadRepository,
            cancellationToken);

        if (validationErrors.Count > 0)
            return OperationResult<ChangeMyUsernameResult>.Failure(validationErrors);

        var mutation = account.ChangeUsername(newUsername);
        if (mutation.IsFailure)
            return OperationResult<ChangeMyUsernameResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        return OperationResult<ChangeMyUsernameResult>.Success(new ChangeMyUsernameResult
        {
            Username = account.Username
        });
    }

    private static Error RequestRequired() =>
        Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    private static Error AccountNotFound() =>
        Error.Create()
            .WithCode(AccountErrorCodes.AccountNotFound)
            .WithMessage("A conta autenticada nao foi encontrada.")
            .Build();
}
