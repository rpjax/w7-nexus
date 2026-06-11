using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Accounts.Errors;

namespace Nexus.Accounts.Application.Services;

public sealed class PasswordValidator : IPasswordValidator
{
    private const int MinLength = 8;

    public Task<IResult> ValidateForCreationAsync(string password)
    {
        return ValidateAsync(password);
    }

    public Task<IResult> ValidateForChangeAsync(string newPassword)
    {
        return ValidateAsync(newPassword);
    }

    private static Task<IResult> ValidateAsync(string password)
    {
        if (string.IsNullOrEmpty(password))
            return Task.FromResult<IResult>(Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordTooShort)
                .WithMessage("A senha não pode estar vazia.")
                .Build()));

        if (password.Length < MinLength)
            return Task.FromResult<IResult>(Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordTooShort)
                .WithMessage($"A senha deve ter no mínimo {MinLength} caracteres.")
                .Build()));

        return Task.FromResult<IResult>(Result.Success());
    }
}
