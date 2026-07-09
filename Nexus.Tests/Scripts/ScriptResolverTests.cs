using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Services;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class ScriptResolverTests
{
    [Fact]
    public async Task ResolveAsync_ByName_ReturnsRuntimeScript()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var script = await scriptRepository.InsertAsync(Script.Create("runtime").Value!);
        await scriptRepository.UpdateAsync(script.PromoteAndReturn(releaseRepository, "console.log('runtime');"));

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest { Name = "runtime" });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("runtime", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task ResolveAsync_ByHost_ExcludesScriptsWithoutHostPatterns()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var runtime = await scriptRepository.InsertAsync(Script.Create("runtime").Value!);
        await scriptRepository.UpdateAsync(runtime.PromoteAndReturn(releaseRepository, "console.log('runtime');"));

        var olx = await scriptRepository.InsertAsync(
            Script.Create("olx", ["*.olx.com.br"]).Value!);
        await scriptRepository.UpdateAsync(olx.PromoteAndReturn(releaseRepository, "console.log('olx');"));

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "www.olx.com.br" });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("olx", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task ResolveAsync_ByHost_OrdersByPriority()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var olxB = await scriptRepository.InsertAsync(
            Script.Create("olx-b", ["olx.com.br"], priority: 10).Value!);
        await scriptRepository.UpdateAsync(olxB.PromoteAndReturn(releaseRepository, "console.log('b');"));

        var olxA = await scriptRepository.InsertAsync(
            Script.Create("olx-a", ["olx.com.br"], priority: 0).Value!);
        await scriptRepository.UpdateAsync(olxA.PromoteAndReturn(releaseRepository, "console.log('a');"));

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });

        Assert.True(result.IsSuccess);
        Assert.Equal(["olx-a", "olx-b"], result.Value!.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task ResolveAsync_SkipsDeprecatedRelease_ByDefault()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var script = await scriptRepository.InsertAsync(Script.Create("olx", ["olx.com.br"]).Value!);
        var release = await releaseRepository.InsertAsync(
            Release.Publish(script.Id, "console.log('olx');", new SemanticVersion(1, 0, 0)).Value!);
        await releaseRepository.UpdateAsync(release.Deprecate().Value!);
        script.Promote(ChannelKey.Production, release.Id);
        await scriptRepository.UpdateAsync(script);

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ResolveAsync_AllowDeprecated_IncludesDeprecatedRelease()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var script = await scriptRepository.InsertAsync(Script.Create("olx", ["olx.com.br"]).Value!);
        var release = await releaseRepository.InsertAsync(
            Release.Publish(script.Id, "console.log('olx');", new SemanticVersion(1, 0, 0)).Value!);
        await releaseRepository.UpdateAsync(release.Deprecate().Value!);
        script.Promote(ChannelKey.Production, release.Id);
        await scriptRepository.UpdateAsync(script);

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest
        {
            Host = "olx.com.br",
            AllowDeprecated = true,
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task ResolveAsync_WithVersion_ReturnsSpecificRelease()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();

        var script = await scriptRepository.InsertAsync(Script.Create("olx", ["olx.com.br"]).Value!);
        var v1 = await releaseRepository.InsertAsync(
            Release.Publish(script.Id, "console.log('v1');", new SemanticVersion(1, 0, 0)).Value!);
        var v2 = await releaseRepository.InsertAsync(
            Release.Publish(script.Id, "console.log('v2');", new SemanticVersion(2, 0, 0)).Value!);
        script.Promote(ChannelKey.Production, v2.Id);
        await scriptRepository.UpdateAsync(script);

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var result = await resolver.ResolveAsync(new ResolveScriptsRequest
        {
            Host = "olx.com.br",
            Version = "1.0.0",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("1.0.0", result.Value.Items[0].Version);
        Assert.Contains("v1", result.Value.Items[0].SourceCode);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidVersion_Fails()
    {
        var resolver = new ScriptResolver(
            new InMemoryScriptRepository(),
            new InMemoryReleaseRepository(),
            new ScriptCache());

        var result = await resolver.ResolveAsync(new ResolveScriptsRequest
        {
            Name = "runtime",
            Version = "invalid",
        });

        Assert.True(result.IsFailure);
    }
}

internal static class ScriptResolverTestExtensions
{
    public static Script PromoteAndReturn(
        this Script script,
        InMemoryReleaseRepository releaseRepository,
        string sourceCode,
        SemanticVersion? version = null)
    {
        var release = releaseRepository.InsertAsync(
            Release.Publish(script.Id, sourceCode, version ?? new SemanticVersion(1, 0, 0)).Value!)
            .GetAwaiter()
            .GetResult();

        script.Promote(ChannelKey.Production, release.Id);
        return script;
    }
}
