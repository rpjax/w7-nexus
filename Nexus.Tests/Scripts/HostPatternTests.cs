using Nexus.Scripts.Aggregates;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class HostPatternTests
{
    [Theory]
    [InlineData("*", "www.olx.com.br", true)]
    [InlineData("*", "anything.example", true)]
    [InlineData("olx.com.br", "olx.com.br", true)]
    [InlineData("olx.com.br", "www.olx.com.br", false)]
    [InlineData("*.olx.com.br", "www.olx.com.br", true)]
    [InlineData("*.olx.com.br", "m.olx.com.br", true)]
    [InlineData("*.olx.com.br", "olx.com.br", false)]
    public void Matches_ReturnsExpected(string pattern, string host, bool expected)
    {
        var hostPattern = HostPattern.Create(pattern).Value!;

        Assert.Equal(expected, hostPattern.Matches(host));
    }

    [Theory]
    [InlineData("*.com")]
    [InlineData("olx.*")]
    [InlineData("*olx.com.br")]
    [InlineData("")]
    [InlineData("https://olx.com.br")]
    public void Create_RejectsInvalidPatterns(string pattern)
    {
        var result = HostPattern.Create(pattern);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_AllowsCatchAll()
    {
        var result = HostPattern.Create("*");

        Assert.True(result.IsSuccess);
    }
}
