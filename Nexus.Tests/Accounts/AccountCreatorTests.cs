using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Infrastructure;
using Nexus.Accounts.Application;
using Xunit;
using Nexus.Legacy.Accounts.Infrastructure;

namespace Nexus.Tests.Accounts;

public sealed class AccountCreatorTests
{
    [Fact]
    public async Task CreateAccountAsync_ValidInput_CreatesAccount()
    {
        var repo = new InMemoryAccountRepository();
        var sut = CreateSut(repo);

        var result = await sut.CreateAccountAsync("newuser", "password123", ["admin"], ["read"]);

        Assert.True(result.IsSuccess);
        var account = result.Value!;
        Assert.NotNull(account.Id);
        Assert.Equal("newuser", account.Username);
        Assert.NotEqual("password123", account.PasswordHash);
        Assert.Equal(["admin"], account.Roles);
        Assert.Equal(["read"], account.Permissions);

        var persisted = repo.AsQueryable().FirstOrDefault(a => a.Id == account.Id);
        Assert.NotNull(persisted);
        Assert.Equal("newuser", persisted.Username);
    }

    [Fact]
    public async Task CreateAccountAsync_InvalidUsername_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        var sut = CreateSut(repo);

        var result = await sut.CreateAccountAsync("ab", "password123");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountAsync_ShortPassword_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        var sut = CreateSut(repo);

        var result = await sut.CreateAccountAsync("validuser", "short");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountAsync_UsernameAlreadyTaken_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        await SeedAccountAsync(repo, "taken", "hash");
        var sut = CreateSut(repo);

        var result = await sut.CreateAccountAsync("taken", "password123");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountAsync_NoRolesOrPermissions_CreatesWithEmpty()
    {
        var repo = new InMemoryAccountRepository();
        var sut = CreateSut(repo);

        var result = await sut.CreateAccountAsync("minimal", "password123");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Roles);
        Assert.Empty(result.Value!.Permissions);
    }

    private static AccountCreator CreateSut(InMemoryAccountRepository repo)
    {
        return new AccountCreator(
            repo,
            new UsernameValidator(repo),
            new PasswordValidator(),
            new PasswordHasher());
    }

    private static async Task SeedAccountAsync(InMemoryAccountRepository repo, string username, string passwordHash, string id = "seed-1")
    {
        var account = new Account(id, username, passwordHash, [], []);
        await repo.CreateAsync(account);
    }
}
