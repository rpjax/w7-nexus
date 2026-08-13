using Aidan.Core.Errors;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Errors;

namespace Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;

internal static class AccountRegistrationPolicy
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 64;
    private const int PasswordMinLength = 8;
    private static readonly HashSet<char> InvalidUsernameChars = new(" @<>\"'/\\".ToCharArray());

    public static async Task<List<Error>> ValidateAsync(
        string username,
        string password,
        IAccountReadRepository accountReadRepository,
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();
        var normalizedUsername = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            errors.Add(BuildError(AccountErrorCodes.UsernameEmpty, "O nome de usuario nao pode estar vazio."));
        }
        else
        {
            if (normalizedUsername.Length < UsernameMinLength || normalizedUsername.Length > UsernameMaxLength)
            {
                errors.Add(BuildError(
                    AccountErrorCodes.UsernameInvalidFormat,
                    $"O nome de usuario deve ter entre {UsernameMinLength} e {UsernameMaxLength} caracteres."));
            }

            if (normalizedUsername.Any(character => InvalidUsernameChars.Contains(character)))
            {
                errors.Add(BuildError(
                    AccountErrorCodes.UsernameInvalidFormat,
                    "O nome de usuario contem caracteres invalidos. Use apenas letras, numeros e os caracteres permitidos."));
            }

            var existing = await accountReadRepository.FindByUsernameAsync(normalizedUsername, cancellationToken);
            if (existing is not null)
            {
                errors.Add(BuildError(
                    AccountErrorCodes.UsernameAlreadyTaken,
                    $"O handle '{normalizedUsername}' ja esta em uso. Escolha outro."));
            }
            else if (await accountReadRepository.IsHandleRetiredAsync(normalizedUsername, cancellationToken))
            {
                errors.Add(BuildError(
                    AccountErrorCodes.HandleRetired,
                    $"O handle '{normalizedUsername}' esta aposentado e nao pode ser reutilizado."));
            }
        }

        if (string.IsNullOrEmpty(password))
        {
            errors.Add(BuildError(AccountErrorCodes.PasswordTooShort, "A senha nao pode estar vazia."));
        }
        else if (password.Length < PasswordMinLength)
        {
            errors.Add(BuildError(
                AccountErrorCodes.PasswordTooShort,
                $"A senha deve ter no minimo {PasswordMinLength} caracteres."));
        }

        return errors;
    }

    public static async Task<List<Error>> ValidateUsernameOnlyAsync(
        string username,
        string? currentUsername,
        IAccountReadRepository accountReadRepository,
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();
        var normalizedUsername = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            errors.Add(BuildError(AccountErrorCodes.UsernameEmpty, "O nome de usuario nao pode estar vazio."));
            return errors;
        }

        if (normalizedUsername.Length < UsernameMinLength || normalizedUsername.Length > UsernameMaxLength)
        {
            errors.Add(BuildError(
                AccountErrorCodes.UsernameInvalidFormat,
                $"O nome de usuario deve ter entre {UsernameMinLength} e {UsernameMaxLength} caracteres."));
        }

        if (normalizedUsername.Any(character => InvalidUsernameChars.Contains(character)))
        {
            errors.Add(BuildError(
                AccountErrorCodes.UsernameInvalidFormat,
                "O nome de usuario contem caracteres invalidos. Use apenas letras, numeros e os caracteres permitidos."));
        }

        if (!string.IsNullOrWhiteSpace(currentUsername)
            && string.Equals(normalizedUsername, currentUsername.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(BuildError(
                AccountErrorCodes.UsernameUnchanged,
                "O novo nome de usuario e igual ao atual."));
            return errors;
        }

        var existing = await accountReadRepository.FindByUsernameAsync(normalizedUsername, cancellationToken);
        if (existing is not null)
        {
            errors.Add(BuildError(
                AccountErrorCodes.UsernameAlreadyTaken,
                $"O handle '{normalizedUsername}' ja esta em uso. Escolha outro."));
        }
        else if (await accountReadRepository.IsHandleRetiredAsync(normalizedUsername, cancellationToken))
        {
            errors.Add(BuildError(
                AccountErrorCodes.HandleRetired,
                $"O handle '{normalizedUsername}' esta aposentado e nao pode ser reutilizado."));
        }

        return errors;
    }

    public static Error BuildError(string code, string message) =>
        Error.Create()
            .WithCode(code)
            .WithMessage(message)
            .Build();
}
