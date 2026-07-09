using Nexus.Scripts.Aggregates;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("12.34.56", 12, 34, 56)]
    public void TryParse_ValidVersion_Succeeds(string input, int major, int minor, int patch)
    {
        var parsed = SemanticVersion.TryParse(input, out var version);

        Assert.True(parsed);
        Assert.NotNull(version);
        Assert.Equal(new SemanticVersion(major, minor, patch), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.0")]
    [InlineData("a.b.c")]
    [InlineData("1.0.-1")]
    public void TryParse_InvalidVersion_Fails(string? input)
    {
        var parsed = SemanticVersion.TryParse(input, out var version);

        Assert.False(parsed);
        Assert.Null(version);
    }
}
