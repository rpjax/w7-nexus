using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Services;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class ScriptAdministratorTests
{
    [Fact]
    public async Task GetScript_ListReleases_GetRelease_UpdateScript_Succeed()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "olx",
            HostPatterns = ["olx.com.br"],
            Priority = 5,
            Description = "patch olx",
        });

        Assert.True(create.IsSuccess);

        var publish = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('v1');" });

        var detail = await administrator.GetScriptAsync(identity, create.Value.Id);
        Assert.True(detail.IsSuccess);
        Assert.Equal("olx", detail.Value!.Name);
        Assert.Single(detail.Value.HostPatterns);
        Assert.Equal(3, detail.Value.Channels.Count);
        Assert.All(detail.Value.Channels, channel => Assert.Null(channel.CurrentReleaseId));

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = publish.Value!.Id });

        detail = await administrator.GetScriptAsync(identity, create.Value.Id);
        var prod = detail.Value!.Channels.First(channel => channel.RouteValue == "prod");
        Assert.Equal("0.0.1", prod.Version);
        Assert.Equal(publish.Value.Id, prod.CurrentReleaseId);

        var releases = await administrator.ListReleasesAsync(identity, create.Value.Id);
        Assert.True(releases.IsSuccess);
        Assert.Single(releases.Value!.Items);

        var release = await administrator.GetReleaseAsync(identity, create.Value.Id, publish.Value.Id);
        Assert.True(release.IsSuccess);
        Assert.Equal("0.0.1", release.Value!.Version);
        Assert.DoesNotContain("SourceCode", release.Value.GetType().GetProperties().Select(property => property.Name));

        var source = await administrator.GetReleaseSourceCodeAsync(identity, create.Value.Id, publish.Value.Id);
        Assert.True(source.IsSuccess);
        Assert.Contains("v1", source.Value!.SourceCode);

        var update = await administrator.UpdateScriptAsync(identity, create.Value.Id, new UpdateScriptRequest
        {
            Priority = 0,
            Description = "updated",
            HostPatterns = ["*.olx.com.br"],
        });

        Assert.True(update.IsSuccess);
        Assert.Equal(0, update.Value!.Priority);
        Assert.Equal("updated", update.Value.Description);
        Assert.Equal(["*.olx.com.br"], update.Value.HostPatterns);
    }

    [Fact]
    public async Task SearchScripts_ReturnsChannelSummaries()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "runtime",
            Priority = 0,
        });

        var publish = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('runtime');" });

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = publish.Value!.Id });

        var search = await administrator.SearchScriptsAsync(identity, new SearchScriptsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = "runtime",
        });

        Assert.True(search.IsSuccess);
        var item = Assert.Single(search.Value!.Items);
        Assert.Equal(3, item.Channels.Count);
        Assert.Equal("0.0.1", item.Channels.First(channel => channel.RouteValue == "prod").Version);
    }

    [Fact]
    public async Task CreateScript_Publish_Promote_Succeeds()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "olx",
            HostPatterns = ["olx.com.br"],
        });

        Assert.True(create.IsSuccess);

        var publish = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('olx');" });

        Assert.True(publish.IsSuccess);

        var promote = await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = publish.Value!.Id });

        Assert.True(promote.IsSuccess);

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var resolved = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });

        Assert.True(resolved.IsSuccess);
        Assert.Single(resolved.Value!.Items);
        Assert.Equal("olx", resolved.Value.Items[0].Name);
    }

    [Fact]
    public async Task DeprecateRelease_InvalidatesResolvedCache()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "olx",
            HostPatterns = ["olx.com.br"],
        });

        var publish = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('olx');" });

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = publish.Value!.Id });

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var before = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });
        Assert.Single(before.Value!.Items);

        var deprecate = await administrator.DeprecateReleaseAsync(identity, create.Value.Id, publish.Value.Id);
        Assert.True(deprecate.IsSuccess);

        var after = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });
        Assert.True(after.IsSuccess);
        Assert.Empty(after.Value!.Items);
    }

    [Fact]
    public async Task RestoreRelease_MakesReleaseResolvableAgain()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "olx",
            HostPatterns = ["olx.com.br"],
        });

        var publish = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('olx');" });

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = publish.Value!.Id });

        await administrator.DeprecateReleaseAsync(identity, create.Value.Id, publish.Value.Id);

        var restore = await administrator.RestoreReleaseAsync(identity, create.Value.Id, publish.Value.Id);
        Assert.True(restore.IsSuccess);

        var resolver = new ScriptResolver(scriptRepository, releaseRepository, cache);
        var resolved = await resolver.ResolveAsync(new ResolveScriptsRequest { Host = "olx.com.br" });

        Assert.True(resolved.IsSuccess);
        Assert.Single(resolved.Value!.Items);
    }

    [Fact]
    public async Task DeleteRelease_ClearsChannelPointersAndRemovesRelease()
    {
        var scriptRepository = new InMemoryScriptRepository();
        var releaseRepository = new InMemoryReleaseRepository();
        var cache = new ScriptCache();
        var administrator = CreateAdministrator(scriptRepository, releaseRepository, cache);
        var identity = AdminIdentity();

        var create = await administrator.CreateScriptAsync(identity, new CreateScriptRequest
        {
            Name = "runtime",
            HostPatterns = [],
        });

        var first = await administrator.PublishReleaseAsync(
            identity,
            create.Value!.Id,
            new PublishReleaseRequest { SourceCode = "console.log('v1');" });

        var second = await administrator.PublishReleaseAsync(
            identity,
            create.Value.Id,
            new PublishReleaseRequest { SourceCode = "console.log('v2');" });

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "prod",
            new PromoteReleaseRequest { ReleaseId = first.Value!.Id });

        await administrator.PromoteReleaseAsync(
            identity,
            create.Value.Id,
            "staging",
            new PromoteReleaseRequest { ReleaseId = second.Value!.Id });

        var delete = await administrator.DeleteReleaseAsync(identity, create.Value.Id, first.Value.Id);

        Assert.True(delete.IsSuccess);
        Assert.Equal(["prod"], delete.Value!.ClearedChannelRouteValues);

        var detail = await administrator.GetScriptAsync(identity, create.Value.Id);
        var prod = detail.Value!.Channels.First(channel => channel.RouteValue == "prod");
        var staging = detail.Value.Channels.First(channel => channel.RouteValue == "staging");

        Assert.Null(prod.CurrentReleaseId);
        Assert.Null(prod.Version);
        Assert.Equal(second.Value.Id, staging.CurrentReleaseId);

        var releases = await administrator.ListReleasesAsync(identity, create.Value.Id);
        Assert.True(releases.IsSuccess);
        Assert.Single(releases.Value!.Items);
        Assert.Equal(second.Value.Id, releases.Value.Items[0].Id);

        var missing = await administrator.GetReleaseAsync(identity, create.Value.Id, first.Value.Id);
        Assert.False(missing.IsSuccess);
    }

    private static ScriptAdministrator CreateAdministrator(
        InMemoryScriptRepository scriptRepository,
        InMemoryReleaseRepository releaseRepository,
        ScriptCache cache) =>
        new(new PermissiveAdministratorAccessPolicy(), scriptRepository, releaseRepository, cache);

    private static RequesterIdentity AdminIdentity() =>
        new("admin-1", [Roles.Administrator], Array.Empty<string>());

    private sealed class PermissiveAdministratorAccessPolicy : IAdministratorAccessPolicy
    {
        public Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity) =>
            Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());
    }
}
