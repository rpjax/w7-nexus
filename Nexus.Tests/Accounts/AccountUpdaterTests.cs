using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Infrastructure;
using Nexus.Accounts.Application;
using Xunit;

using Nexus.Accounts.ErrorCodes;

namespace Nexus.Tests.Accounts;

public sealed class AccountUpdaterTests
{
    [Fact]
    public async Task UpdateUsernameAsync_ValidChange_UpdatesUsername()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "oldname", "hash");
        var sut = CreateSut(repo);

        var result = await sut.UpdateUsernameAsync(accountId, "newname");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.Equal("newname", updated.Username);
    }

    [Fact]
    public async Task UpdateUsernameAsync_AccountNotFound_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        var sut = CreateSut(repo);

        var result = await sut.UpdateUsernameAsync("nonexistent", "newname");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task UpdateUsernameAsync_UsernameAlreadyTaken_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user1", "hash");
        await SeedAccountAsync(repo, "user2", "hash", "id-2");
        var sut = CreateSut(repo);

        var result = await sut.UpdateUsernameAsync(accountId, "user2");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdatePasswordAsync_Valid_UpdatesPassword()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user", "oldhash");
        var sut = CreateSut(repo);

        var result = await sut.UpdatePasswordAsync(accountId, "newpassword123");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.NotEqual("oldhash", updated.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword123", updated.PasswordHash));
    }

    [Fact]
    public async Task AddRoleAsync_Valid_AddsRole()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user", "hash");
        var sut = CreateSut(repo);

        var result = await sut.AddRoleAsync(accountId, "admin");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.Contains("admin", updated.Roles);
    }

    [Fact]
    public async Task RemoveRoleAsync_ExistingRole_RemovesRole()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user", "hash");
        await AddRoleToAccountAsync(repo, accountId, "admin");
        var sut = CreateSut(repo);

        var result = await sut.RemoveRoleAsync(accountId, "admin");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.DoesNotContain("admin", updated.Roles);
    }

    [Fact]
    public async Task AddPermissionAsync_Valid_AddsPermission()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user", "hash");
        var sut = CreateSut(repo);

        var result = await sut.AddPermissionAsync(accountId, "read:users");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.Contains("read:users", updated.Permissions);
    }

    [Fact]
    public async Task RemovePermissionAsync_Existing_RemovesPermission()
    {
        var repo = new InMemoryAccountRepository();
        var accountId = await SeedAccountAsync(repo, "user", "hash");
        await AddPermissionToAccountAsync(repo, accountId, "read:users");
        var sut = CreateSut(repo);

        var result = await sut.RemovePermissionAsync(accountId, "read:users");

        Assert.True(result.IsSuccess);
        var updated = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        Assert.NotNull(updated);
        Assert.DoesNotContain("read:users", updated.Permissions);
    }

    private static AccountUpdater CreateSut(InMemoryAccountRepository repo)
    {
        return new AccountUpdater(
            repo,
            new UsernameValidator(repo),
            new PasswordValidator(),
            new PasswordHasher());
    }

    private static async Task<string> SeedAccountAsync(InMemoryAccountRepository repo, string username, string passwordHash, string? id = null)
    {
        id ??= Guid.NewGuid().ToString();
        var account = new Account(id, username, passwordHash, [], []);
        await repo.CreateAsync(account);
        return id;
    }

    private static async Task AddRoleToAccountAsync(InMemoryAccountRepository repo, string accountId, string role)
    {
        var account = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        if (account is null) return;
        account.AddRole(role);
        await repo.UpdateAsync(account);
    }

    private static async Task AddPermissionToAccountAsync(InMemoryAccountRepository repo, string accountId, string permission)
    {
        var account = repo.AsQueryable().FirstOrDefault(a => a.Id == accountId);
        if (account is null) return;
        account.AddPermission(permission);
        await repo.UpdateAsync(account);
    }
}
