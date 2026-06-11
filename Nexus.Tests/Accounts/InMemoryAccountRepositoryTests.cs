using Nexus.Accounts.Aggregates;
using Xunit;

namespace Nexus.Tests.Accounts;

public sealed class InMemoryAccountRepositoryTests
{
    [Fact]
    public async Task CreateAsync_StoresAccount()
    {
        var repo = new InMemoryAccountRepository();
        var account = new Account("id-1", "user", "hash", ["admin"], ["read"]);

        await repo.CreateAsync(account);

        var found = repo.AsQueryable().FirstOrDefault(a => a.Id == "id-1");
        Assert.NotNull(found);
        Assert.Equal("user", found.Username);
        Assert.Equal("hash", found.PasswordHash);
        Assert.Equal(["admin"], found.Roles);
        Assert.Equal(["read"], found.Permissions);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAccount()
    {
        var repo = new InMemoryAccountRepository();
        var account = new Account("id-1", "user", "hash", [], []);
        await repo.CreateAsync(account);
        account.ChangeUsername("updated");

        await repo.UpdateAsync(account);

        var found = repo.AsQueryable().FirstOrDefault(a => a.Id == "id-1");
        Assert.NotNull(found);
        Assert.Equal("updated", found.Username);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccount()
    {
        var repo = new InMemoryAccountRepository();
        var account = new Account("id-1", "user", "hash", [], []);
        await repo.CreateAsync(account);

        await repo.DeleteAsync(account);

        var found = repo.AsQueryable().FirstOrDefault(a => a.Id == "id-1");
        Assert.Null(found);
    }

    [Fact]
    public async Task AsQueryable_SupportsWhereAndFirstOrDefault()
    {
        var repo = new InMemoryAccountRepository();
        await repo.CreateAsync(new Account("1", "alice", "hash", [], []));
        await repo.CreateAsync(new Account("2", "bob", "hash", [], []));

        var bob = repo.AsQueryable().FirstOrDefault(a => a.Username == "bob");

        Assert.NotNull(bob);
        Assert.Equal("2", bob.Id);
    }

    [Fact]
    public async Task AsQueryable_SupportsAny()
    {
        var repo = new InMemoryAccountRepository();
        await repo.CreateAsync(new Account("1", "user", "hash", [], []));

        var exists = repo.AsQueryable().Any(a => a.Username == "user");
        var notExists = repo.AsQueryable().Any(a => a.Username == "nobody");

        Assert.True(exists);
        Assert.False(notExists);
    }
}
