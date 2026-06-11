using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Accounts.Errors;

namespace Nexus.Accounts.Application.Services;

public sealed class UsernameValidator : IUsernameValidator
{
    private const int MinLength = 3;
    private const int MaxLength = 64;
    private static readonly HashSet<char> InvalidChars = new(" @<>\"'/\\".ToCharArray());

    private readonly IAccountRepository _accounts;

    public UsernameValidator(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public Task<IResult> ValidateForCreationAsync(string username)
    {
        var formatResult = ValidateFormat(username);
        if (formatResult.IsFailure)
            return Task.FromResult<IResult>(formatResult);

        var existing = _accounts.AsQueryable()
            .FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return Task.FromResult<IResult>(Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameAlreadyTaken)
                .WithMessage($"O nome de usuário '{username}' já está em uso. Escolha outro nome.")
                .Build()));

        return Task.FromResult<IResult>(Result.Success());
    }

    public Task<IResult> ValidateForChangeAsync(string newUsername, string accountId)
    {
        var formatResult = ValidateFormat(newUsername);
        if (formatResult.IsFailure)
            return Task.FromResult<IResult>(formatResult);

        var existing = _accounts.AsQueryable()
            .FirstOrDefault(a => string.Equals(a.Username, newUsername, StringComparison.OrdinalIgnoreCase));
        if (existing is not null && existing.Id != accountId)
            return Task.FromResult<IResult>(Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameAlreadyTaken)
                .WithMessage($"O nome de usuário '{newUsername}' já está em uso. Escolha outro nome.")
                .Build()));

        return Task.FromResult<IResult>(Result.Success());
    }

    private static IResult ValidateFormat(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameEmpty)
                .WithMessage("O nome de usuário não pode estar vazio.")
                .Build());

        if (username.Length < MinLength || username.Length > MaxLength)
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameInvalidFormat)
                .WithMessage($"O nome de usuário deve ter entre {MinLength} e {MaxLength} caracteres.")
                .Build());

        if (username.Any(c => InvalidChars.Contains(c)))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameInvalidFormat)
                .WithMessage("O nome de usuário contém caracteres inválidos. Use apenas letras, números e os caracteres permitidos.")
                .Build());

        return Result.Success();
    }
}
