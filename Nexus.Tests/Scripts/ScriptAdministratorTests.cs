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
