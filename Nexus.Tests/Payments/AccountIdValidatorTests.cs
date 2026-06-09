using Nexus.Legacy.Accounts.Aggregates;
using Nexus.Payments.Infrastructure;
using Nexus.Tests.Accounts;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class AccountIdValidatorTests
{
    [Fact]
    public async Task ExistsAsync_WhenAccountInRepository_ReturnsTrue()
    {
        var repo = new InMemoryAccountRepository();
        var id = "acc-123";
        await repo.CreateAsync(CreateAccount(id, "user1"));
        var sut = new AccountIdValidator(repo);

        var exists = await sut.ExistsAsync(id);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WhenMissing_ReturnsFalse()
    {
        var repo = new InMemoryAccountRepository();
        var sut = new AccountIdValidator(repo);

        var exists = await sut.ExistsAsync("unknown");

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_IsCaseSensitive_PerRepositoryId()
    {
        var repo = new InMemoryAccountRepository();
        await repo.CreateAsync(CreateAccount("AbC", "u"));
        var sut = new AccountIdValidator(repo);

        Assert.True(await sut.ExistsAsync("AbC"));
        Assert.False(await sut.ExistsAsync("abc"));
    }

    private static Account CreateAccount(string id, string username) =>
        new(
            id,
            username,
            "hash",
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
}
