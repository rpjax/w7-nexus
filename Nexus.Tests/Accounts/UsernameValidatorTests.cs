using Nexus.Accounts.Aggregates;
using Nexus.Accounts.ErrorCodes;
using Nexus.Accounts.Infrastructure;
using Nexus.Accounts.Application;
using Xunit;

namespace Nexus.Tests.Accounts;

public sealed class UsernameValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task ValidateForCreationAsync_EmptyOrNull_ReturnsFailure(string? username)
    {
        var repo = new InMemoryAccountRepository();
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForCreationAsync(username ?? "");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.UsernameEmpty);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public async Task ValidateForCreationAsync_TooShort_ReturnsFailure(string username)
    {
        var repo = new InMemoryAccountRepository();
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForCreationAsync(username);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.UsernameInvalidFormat);
    }

    [Theory]
    [InlineData("user@name")]
    [InlineData("user name")]
    [InlineData("user<name")]
    public async Task ValidateForCreationAsync_InvalidChars_ReturnsFailure(string username)
    {
        var repo = new InMemoryAccountRepository();
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForCreationAsync(username);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.UsernameInvalidFormat);
    }

    [Fact]
    public async Task ValidateForCreationAsync_UsernameAlreadyExists_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        await SeedAccountAsync(repo, "existing", "hash");
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForCreationAsync("existing");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.UsernameAlreadyTaken);
    }

    [Fact]
    public async Task ValidateForCreationAsync_ValidAndAvailable_ReturnsSuccess()
    {
        var repo = new InMemoryAccountRepository();
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForCreationAsync("validuser");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateForChangeAsync_UsernameTakenByAnother_ReturnsFailure()
    {
        var repo = new InMemoryAccountRepository();
        await SeedAccountAsync(repo, "taken", "hash", "id-1");
        await SeedAccountAsync(repo, "other", "hash", "id-2");
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForChangeAsync("taken", "id-2");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.UsernameAlreadyTaken);
    }

    [Fact]
    public async Task ValidateForChangeAsync_UsernameTakenBySelf_ReturnsSuccess()
    {
        var repo = new InMemoryAccountRepository();
        await SeedAccountAsync(repo, "myuser", "hash", "my-id");
        var sut = new UsernameValidator(repo);

        var result = await sut.ValidateForChangeAsync("myuser", "my-id");

        Assert.True(result.IsSuccess);
    }

    private static async Task SeedAccountAsync(InMemoryAccountRepository repo, string username, string passwordHash, string id = "seed-1")
    {
        var account = new Account(id, username, passwordHash, [], []);
        await repo.CreateAsync(account);
    }
}
