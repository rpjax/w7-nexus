using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application;
using Nexus.Accounts.ErrorCodes;

namespace Nexus.Accounts.Infrastructure;

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
                .WithMessage("Password cannot be empty")
                .Build()));

        if (password.Length < MinLength)
            return Task.FromResult<IResult>(Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordTooShort)
                .WithMessage($"Password must be at least {MinLength} characters")
                .Build()));

        return Task.FromResult<IResult>(Result.Success());
    }
}
