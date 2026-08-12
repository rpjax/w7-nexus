using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;
using Refactor.Nexus.Api.Authentication.Domain.Errors;

namespace Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpUser;

public sealed record SignUpUserCommand(string Username, string Password);

public sealed class SignUpUserResult
{
    public required string AccountId { get; init; }
    public required AuthenticationTokens Tokens { get; init; }
}

public sealed class SignUpUserHandler : ISignUpUserUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public SignUpUserHandler(
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<IOperationResult<SignUpUserResult>> HandleAsync(
        SignUpUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<SignUpUserResult>.Failure(RequestRequiredError());

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            command.Username,
            command.Password,
            _accountReadRepository,
            cancellationToken);

        if (errors.Count > 0)
            return OperationResult<SignUpUserResult>.Failure(errors);

        var passwordHash = await _passwordHasher.HashAsync(command.Password, cancellationToken);
        var account = Account.Create(command.Username.Trim(), passwordHash);
        account = await _accountRepository.CreateAsync(account, cancellationToken);

        return OperationResult<SignUpUserResult>.Success(new SignUpUserResult
        {
            AccountId = account.Id.ToString(),
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
        AccountRegistrationPolicy.BuildError(AuthenticationErrorCodes.RequestRequired, "O corpo da requisicao e obrigatorio.");
}
