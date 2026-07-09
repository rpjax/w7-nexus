using Nexus.Scripts.Aggregates;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class ScriptAggregateTests
{
    [Fact]
    public void Create_WithoutHostPatterns_Succeeds()
    {
        var result = Script.Create("runtime");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Scope);
        Assert.False(result.Value.HasHostPatterns());
    }

    [Fact]
    public void Create_WithHostPatterns_Succeeds()
    {
        var result = Script.Create(
            "olx",
            hostPatterns: ["*.olx.com.br", "olx.com.br"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Channels.Count);
        Assert.True(result.Value.HasHostPatterns());
    }

    [Fact]
    public void Create_WithInvalidHostPattern_Fails()
    {
        var result = Script.Create("olx", hostPatterns: ["*.com"]);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Promote_SetsChannelReleaseId()
    {
        var script = Script.Create("olx", ["olx.com.br"]).Value!;

        var promoteResult = script.Promote(ChannelKey.Production, "release-1");

        Assert.True(promoteResult.IsSuccess);
        Assert.Equal("release-1", script.FindChannel(ChannelKey.Production)!.CurrentReleaseId);
    }

    [Fact]
    public void AddCustomChannel_AddsUniqueChannel()
    {
        var script = Script.Create("olx", ["olx.com.br"]).Value!;

        var addResult = script.AddCustomChannel("someTest");

        Assert.True(addResult.IsSuccess);
        Assert.Equal(4, script.Channels.Count);
        Assert.NotNull(script.FindChannel(ChannelKey.Parse("someTest").Value!));
    }

    [Fact]
    public void UpdateScope_WithEmptyPatterns_ClearsScope()
    {
        var script = Script.Create("olx", ["olx.com.br"]).Value!;

        var updateResult = script.UpdateScope([]);

        Assert.True(updateResult.IsSuccess);
        Assert.Null(script.Scope);
        Assert.False(script.HasHostPatterns());
    }
}
