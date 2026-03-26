using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.ErrorCodes;
using Nexus.Accounts.Application;

namespace Nexus.Accounts.Infrastructure;

public sealed class AccountUpdater : IAccountUpdater
{
    private readonly IAccountRepository _accounts;
    private readonly IUsernameValidator _usernameValidator;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IPasswordHasher _passwordHasher;

    public AccountUpdater(
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

    public async Task<IResult> UpdateUsernameAsync(string accountId, string newUsername)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var validationResult = await _usernameValidator.ValidateForChangeAsync(newUsername, accountId);
        if (validationResult.IsFailure)
            return validationResult;

        var result = account.ChangeUsername(newUsername);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> UpdatePasswordAsync(string accountId, string newPassword)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var passwordResult = await _passwordValidator.ValidateForChangeAsync(newPassword);
        if (passwordResult.IsFailure)
            return passwordResult;

        var passwordHash = await _passwordHasher.HashAsync(newPassword);
        var result = account.ChangePassword(passwordHash);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> AddRoleAsync(string accountId, string role)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var result = account.AddRole(role);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> RemoveRoleAsync(string accountId, string role)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var result = account.RemoveRole(role);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> ClearRolesAsync(string accountId)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        account.ClearRoles();
        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> AddPermissionAsync(string accountId, string permission)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var result = account.AddPermission(permission);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> RemovePermissionAsync(string accountId, string permission)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        var result = account.RemovePermission(permission);
        if (result.IsFailure)
            return result;

        await SaveAccountAsync(account);
        return Result.Success();
    }

    public async Task<IResult> ClearPermissionsAsync(string accountId)
    {
        var account = await LoadAccountAsync(accountId);
        if (account is null)
            return NotFoundResult(accountId);

        account.ClearPermissions();
        await SaveAccountAsync(account);
        return Result.Success();
    }

    private Task<Account?> LoadAccountAsync(string accountId)
    {
        var account = _accounts.AsQueryable()
            .FirstOrDefault(a => a.Id == accountId);
        return Task.FromResult(account);
    }

    private Task SaveAccountAsync(Account account) =>
        _accounts.UpdateAsync(account);

    private static IResult NotFoundResult(string accountId)
    {
        return Result.Failure(Error.Create()
            .WithCode(AccountErrorCodes.AccountNotFound)
            .WithMessage($"Account '{accountId}' was not found")
            .Build());
    }
}
