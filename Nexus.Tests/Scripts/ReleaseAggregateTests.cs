using Nexus.Scripts.Aggregates;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class ReleaseAggregateTests
{
    [Fact]
    public void Publish_ComputesStableHash()
    {
        var version = new SemanticVersion(1, 0, 0);
        var first = Release.Publish("script-1", "console.log('a');", version);
        var second = Release.Publish("script-1", "console.log('a');", version);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Hash.Value, second.Value!.Hash.Value);
        Assert.False(first.Value.IsDeprecated);
    }

    [Fact]
    public void Publish_WithoutSourceCode_Fails()
    {
        var result = Release.Publish("script-1", "", new SemanticVersion(1, 0, 0));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Deprecate_SetsFlag()
    {
        var release = Release.Publish("script-1", "console.log('a');", new SemanticVersion(1, 0, 0)).Value!;

        var deprecated = release.Deprecate();

        Assert.True(deprecated.IsSuccess);
        Assert.True(deprecated.Value!.IsDeprecated);
    }

    [Fact]
    public void Restore_ClearsFlag()
    {
        var release = Release.Publish("script-1", "console.log('a');", new SemanticVersion(1, 0, 0)).Value!;
        var deprecated = release.Deprecate().Value!;

        var restored = deprecated.Restore();

        Assert.True(restored.IsSuccess);
        Assert.False(restored.Value!.IsDeprecated);
    }
}
