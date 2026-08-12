using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;
using Refactor.Nexus.Api.Authentication.Domain.Errors;

namespace Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignIn;

public sealed record SignInCommand(string Username, string Password);

public sealed class SignInResult
{
    public required AuthenticationTokens Tokens { get; init; }
}

public sealed class SignInHandler : ISignInUseCase
{
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IJwtTokenService _jwtTokenService;

    public SignInHandler(
        IAccountReadRepository accountReadRepository,
        IPasswordVerifier passwordVerifier,
        IJwtTokenService jwtTokenService)
    {
        _accountReadRepository = accountReadRepository;
        _passwordVerifier = passwordVerifier;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<IOperationResult<SignInResult>> HandleAsync(
        SignInCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<SignInResult>.Failure(RequestRequiredError());

        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
            return OperationResult<SignInResult>.Failure(InvalidCredentialsError());

        var account = await _accountReadRepository.FindByUsernameAsync(command.Username.Trim(), cancellationToken);
        if (account is null)
            return OperationResult<SignInResult>.Failure(InvalidCredentialsError());

        if (account.IsDisabled)
            return OperationResult<SignInResult>.Failure(AccountDisabledError());

        var passwordValid = await _passwordVerifier.VerifyAsync(command.Password, account.PasswordHash, cancellationToken);
        if (!passwordValid)
            return OperationResult<SignInResult>.Failure(InvalidCredentialsError());

        return OperationResult<SignInResult>.Success(new SignInResult
        {
            Tokens = _jwtTokenService.GenerateTokens(ToTokenSubject(account))
        });
    }

    private static JwtTokenSubject ToTokenSubject(Account account) =>
        new()
        {
            AccountId = account.Id.ToString(),
            Username = account.Username,
            Roles = account.Roles.ToArray(),
            Permissions = account.Permissions.ToArray()
        };

    private static Error RequestRequiredError() =>
        Error.Create()
            .WithCode(AuthenticationErrorCodes.RequestRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    private static Error InvalidCredentialsError() =>
        Error.Create()
            .WithCode(AuthenticationErrorCodes.InvalidCredentials)
            .WithMessage("Usuario ou senha incorretos. Verifique os dados informados e tente novamente.")
            .Build();

    private static Error AccountDisabledError() =>
        Error.Create()
            .WithCode(AuthenticationErrorCodes.AccountDisabled)
            .WithMessage("Esta conta esta desabilitada. Contate um administrador.")
            .Build();
}
