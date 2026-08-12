using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyPassword;

public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword);

public sealed class ChangeMyPasswordResult
{
    public required AuthenticationTokens Tokens { get; init; }
}

public sealed class ChangeMyPasswordHandler : IChangeMyPasswordUseCase
{
    private const int PasswordMinLength = 8;

    private readonly IRequestContext _requestContext;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public ChangeMyPasswordHandler(
        IRequestContext requestContext,
        IAccountRepository accountRepository,
        IPasswordVerifier passwordVerifier,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _requestContext = requestContext;
        _accountRepository = accountRepository;
        _passwordVerifier = passwordVerifier;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<IOperationResult<ChangeMyPasswordResult>> HandleAsync(
        ChangeMyPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ChangeMyPasswordResult>.Failure(RequestRequired());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ChangeMyPasswordResult>.Failure(requesterResult.Errors);

        if (string.IsNullOrEmpty(command.NewPassword) || command.NewPassword.Length < PasswordMinLength)
        {
            return OperationResult<ChangeMyPasswordResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordTooShort)
                .WithMessage($"A nova senha deve ter no minimo {PasswordMinLength} caracteres.")
                .Build());
        }

        if (!AccountId.TryParse(requester.AccountId, out var accountId))
            return OperationResult<ChangeMyPasswordResult>.Failure(AccountNotFound());

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<ChangeMyPasswordResult>.Failure(AccountNotFound());

        if (account.IsDisabled)
        {
            return OperationResult<ChangeMyPasswordResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountDisabled)
                .WithMessage("Esta conta esta desabilitada.")
                .Build());
        }

        var currentValid = await _passwordVerifier.VerifyAsync(
            command.CurrentPassword ?? string.Empty,
            account.PasswordHash,
            cancellationToken);

        if (!currentValid)
        {
            return OperationResult<ChangeMyPasswordResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.CurrentPasswordInvalid)
                .WithMessage("A senha atual esta incorreta.")
                .Build());
        }

        var newHash = await _passwordHasher.HashAsync(command.NewPassword, cancellationToken);
        var mutation = account.ChangePassword(newHash);
        if (mutation.IsFailure)
            return OperationResult<ChangeMyPasswordResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        return OperationResult<ChangeMyPasswordResult>.Success(new ChangeMyPasswordResult
        {
            Tokens = _jwtTokenService.GenerateTokens(new JwtTokenSubject
            {
                AccountId = account.Id.ToString(),
                Username = account.Username,
                Roles = account.Roles.ToArray(),
                Permissions = account.Permissions.ToArray()
            })
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
