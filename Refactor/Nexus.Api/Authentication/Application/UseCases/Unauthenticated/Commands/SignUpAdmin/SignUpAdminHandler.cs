using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;
using Refactor.Nexus.Api.Authentication.Domain.Errors;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpAdmin;

public sealed record SignUpAdminCommand(string Username, string Password, string? AdministratorCreationToken);

public sealed class SignUpAdminResult
{
    public required string AccountId { get; init; }
    public required AuthenticationTokens Tokens { get; init; }
}

public sealed class SignUpAdminHandler : ISignUpAdminUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAdministratorCreationTokenService _administratorCreationTokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IJournalWriter _journal;

    public SignUpAdminHandler(
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository,
        IPasswordHasher passwordHasher,
        IAdministratorCreationTokenService administratorCreationTokenService,
        IJwtTokenService jwtTokenService,
        IJournalWriter journal)
    {
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _passwordHasher = passwordHasher;
        _administratorCreationTokenService = administratorCreationTokenService;
        _jwtTokenService = jwtTokenService;
        _journal = journal;
    }

    public async Task<IOperationResult<SignUpAdminResult>> HandleAsync(
        SignUpAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<SignUpAdminResult>.Failure(RequestRequiredError());

        var tokenAuthorized = await _administratorCreationTokenService.IsAuthorizedAsync(
            command.AdministratorCreationToken,
            cancellationToken);

        if (!tokenAuthorized)
        {
            return OperationResult<SignUpAdminResult>.Unauthorized(AccountRegistrationPolicy.BuildError(
                AccountErrorCodes.AdministratorCreationTokenInvalid,
                "O token especial para criacao de conta admin e invalido ou nao foi informado."));
        }

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            command.Username,
            command.Password,
            _accountReadRepository,
            cancellationToken);

        if (errors.Count > 0)
            return OperationResult<SignUpAdminResult>.Failure(errors);

        var passwordHash = await _passwordHasher.HashAsync(command.Password, cancellationToken);
        var account = Account.Create(
            command.Username.Trim(),
            passwordHash,
            [global::Refactor.Nexus.Api.Authorization.Roles.Administrator]);

        account = await _accountRepository.CreateAsync(account, cancellationToken);
        _journal.RecordCreated(account);

        return OperationResult<SignUpAdminResult>.Success(new SignUpAdminResult
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
