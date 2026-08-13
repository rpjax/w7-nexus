using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.DTOs;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string Username,
    string Password,
    string AccountType,
    string? AdministratorCreationToken);

public sealed class CreateAccountResult
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class CreateAccountHandler : ICreateAccountUseCase
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 64;
    private const int PasswordMinLength = 8;
    private static readonly HashSet<char> InvalidUsernameChars = new(" @<>\"'/\\".ToCharArray());
    private static readonly string[] AllowedAccountTypes = ["admin", "usuario"];

    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAdministratorCreationTokenService _administratorCreationTokenService;
    private readonly IJournalWriter _journal;

    public CreateAccountHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository,
        IPasswordHasher passwordHasher,
        IAdministratorCreationTokenService administratorCreationTokenService,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _passwordHasher = passwordHasher;
        _administratorCreationTokenService = administratorCreationTokenService;
        _journal = journal;
    }

    public async Task<IOperationResult<CreateAccountResult>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<CreateAccountResult>.Failure(RequestBodyRequiredError());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<CreateAccountResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<CreateAccountResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<CreateAccountResult>.Unauthorized(authorization.AuthorizationErrors);

        var validationErrors = await ValidateAsync(command, cancellationToken);
        if (validationErrors.Count > 0)
            return OperationResult<CreateAccountResult>.Failure(validationErrors);

        if (IsAdministratorAccount(command.AccountType))
        {
            var tokenAuthorized = await _administratorCreationTokenService.IsAuthorizedAsync(
                command.AdministratorCreationToken,
                cancellationToken);

            if (!tokenAuthorized)
            {
                return OperationResult<CreateAccountResult>.Unauthorized(BuildError(
                    AccountErrorCodes.AdministratorCreationTokenInvalid,
                    "O token especial para criacao de conta admin e invalido ou nao foi informado."));
            }
        }

        var passwordHash = await _passwordHasher.HashAsync(command.Password, cancellationToken);
        var account = Account.Create(
            command.Username.Trim(),
            passwordHash,
            ResolveRoles(command.AccountType));

        account = await _accountRepository.CreateAsync(account, cancellationToken);
        _journal.RecordCreated(account);

        return OperationResult<CreateAccountResult>.Success(new CreateAccountResult
        {
            Account = AccountDetailsView.FromAccount(account)
        });
    }

    private async Task<List<Error>> ValidateAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<Error>();
        var username = command.Username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add(BuildError(AccountErrorCodes.UsernameEmpty, "O nome de usuario nao pode estar vazio."));
        }
        else
        {
            if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
                errors.Add(BuildError(AccountErrorCodes.UsernameInvalidFormat, $"O nome de usuario deve ter entre {UsernameMinLength} e {UsernameMaxLength} caracteres."));

            if (username.Any(character => InvalidUsernameChars.Contains(character)))
                errors.Add(BuildError(AccountErrorCodes.UsernameInvalidFormat, "O nome de usuario contem caracteres invalidos. Use apenas letras, numeros e os caracteres permitidos."));

            var existing = await _accountReadRepository.FindByUsernameAsync(username, cancellationToken);
            if (existing is not null)
                errors.Add(BuildError(AccountErrorCodes.UsernameAlreadyTaken, $"O handle '{username}' ja esta em uso. Escolha outro."));
            else if (await _accountReadRepository.IsHandleRetiredAsync(username, cancellationToken))
                errors.Add(BuildError(AccountErrorCodes.HandleRetired, $"O handle '{username}' esta aposentado e nao pode ser reutilizado."));
        }

        if (string.IsNullOrEmpty(command.Password))
        {
            errors.Add(BuildError(AccountErrorCodes.PasswordTooShort, "A senha nao pode estar vazia."));
        }
        else if (command.Password.Length < PasswordMinLength)
        {
            errors.Add(BuildError(AccountErrorCodes.PasswordTooShort, $"A senha deve ter no minimo {PasswordMinLength} caracteres."));
        }

        if (!AllowedAccountTypes.Contains((command.AccountType ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase))
            errors.Add(BuildError(AccountErrorCodes.AccountTypeInvalid, "O tipo da conta deve ser 'admin' ou 'usuario'."));

        return errors;
    }

    private static string[] ResolveRoles(string accountType)
    {
        if (IsAdministratorAccount(accountType))
            return [global::Refactor.Nexus.Api.Authorization.Roles.Administrator];

        return Array.Empty<string>();
    }

    private static bool IsAdministratorAccount(string? accountType) =>
        string.Equals(accountType?.Trim(), "admin", StringComparison.OrdinalIgnoreCase);

    private static Error BuildError(string code, string message) =>
        Error.Create()
            .WithCode(code)
            .WithMessage(message)
            .Build();

    private static Error RequestBodyRequiredError() =>
        BuildError(AccountErrorCodes.RequestBodyRequired, "O corpo da requisicao e obrigatorio.");
}
