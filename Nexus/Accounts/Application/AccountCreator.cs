using Aidan.Core.Patterns;
using MongoDB.Bson;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application;

public sealed class AccountCreator : IAccountCreator
{
    private readonly IAccountRepository _accounts;
    private readonly IUsernameValidator _usernameValidator;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IPasswordHasher _passwordHasher;

    public AccountCreator(
        IAccountRepository accounts,
        IUsernameValidator usernameValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher)
    {
        _accounts = accounts;
        _usernameValidator = usernameValidator;
        _passwordValidator = passwordValidator;
        _passwordHasher = passwordHasher;
    }

    public async Task<IResult<Account>> CreateAccountAsync(
        string username,
        string password,
        string[]? roles = null,
        string[]? permissions = null)
    {
        var usernameResult = await _usernameValidator.ValidateForCreationAsync(username);
        if (usernameResult.IsFailure)
            return Result.Create<Account>().WithErrors(usernameResult.Errors).Build();

        var passwordResult = await _passwordValidator.ValidateForCreationAsync(password);
        if (passwordResult.IsFailure)
            return Result.Create<Account>().WithErrors(passwordResult.Errors).Build();

        var passwordHash = await _passwordHasher.HashAsync(password);

        var id = ObjectId.GenerateNewId().ToString();
        var account = new Account(
            id,
            username,
            passwordHash,
            roles ?? Array.Empty<string>(),
            permissions ?? Array.Empty<string>());

        await _accounts.CreateAsync(account);

        return Result.Create<Account>().WithValue(account).Build();
    }
}
