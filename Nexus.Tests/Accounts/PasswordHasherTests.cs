using Nexus.Accounts.Infrastructure;
using Xunit;

namespace Nexus.Tests.Accounts;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public async Task HashAsync_ReturnsNonEmptyHash()
    {
        var hash = await _sut.HashAsync("mypassword");

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.NotEqual("mypassword", hash);
    }

    [Fact]
    public async Task HashAsync_DifferentCallsProduceDifferentHashes()
    {
        var hash1 = await _sut.HashAsync("password");
        var hash2 = await _sut.HashAsync("password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task HashAsync_ProducesValidBCryptHash()
    {
        var hash = await _sut.HashAsync("test123");

        Assert.StartsWith("$2", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("test123", hash));
    }
}
