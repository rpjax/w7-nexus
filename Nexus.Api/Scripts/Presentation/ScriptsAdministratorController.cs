using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;

namespace Nexus.Scripts.Presentation;

[Route("api/scripts/administrator")]
[Authorize]
public sealed class ScriptsAdministratorController : NexusController
{
    private readonly IScriptAdministrator _administrator;
    private readonly IRequesterIdentityResolver _identityResolver;

    public ScriptsAdministratorController(
        IScriptAdministrator administrator,
        IRequesterIdentityResolver identityResolver)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
    }

    [HttpPost]
    public async Task<ActionResult> CreateScriptAsync(
        [FromBody] CreateScriptRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.CreateScriptAsync(identity, request, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult> SearchScriptsAsync(
        [FromQuery] SearchScriptsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.SearchScriptsAsync(identity, request, cancellationToken));
    }

    [HttpGet("{scriptId}")]
    public async Task<ActionResult> GetScriptAsync(
        string scriptId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.GetScriptAsync(identity, scriptId, cancellationToken));
    }

    [HttpPatch("{scriptId}")]
    public async Task<ActionResult> UpdateScriptAsync(
        string scriptId,
        [FromBody] UpdateScriptRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.UpdateScriptAsync(identity, scriptId, request, cancellationToken));
    }

    [HttpGet("{scriptId}/releases")]
    public async Task<ActionResult> ListReleasesAsync(
        string scriptId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.ListReleasesAsync(identity, scriptId, cancellationToken));
    }

    [HttpGet("{scriptId}/releases/{releaseId}")]
    public async Task<ActionResult> GetReleaseAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.GetReleaseAsync(identity, scriptId, releaseId, cancellationToken));
    }

    [HttpGet("{scriptId}/releases/{releaseId}/source-code")]
    public async Task<ActionResult> GetReleaseSourceCodeAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.GetReleaseSourceCodeAsync(identity, scriptId, releaseId, cancellationToken));
    }

    [HttpPost("{scriptId}/releases")]
    public async Task<ActionResult> PublishReleaseAsync(
        string scriptId,
        [FromBody] PublishReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.PublishReleaseAsync(identity, scriptId, request, cancellationToken));
    }

    [HttpPost("{scriptId}/channels/{channelRouteValue}/promote")]
    public async Task<ActionResult> PromoteReleaseAsync(
        string scriptId,
        string channelRouteValue,
        [FromBody] PromoteReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.PromoteReleaseAsync(
            identity,
            scriptId,
            channelRouteValue,
            request,
            cancellationToken));
    }

    [HttpPost("{scriptId}/channels")]
    public async Task<ActionResult> AddCustomChannelAsync(
        string scriptId,
        [FromBody] AddCustomChannelRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.AddCustomChannelAsync(identity, scriptId, request, cancellationToken));
    }

    [HttpPost("{scriptId}/releases/{releaseId}/deprecate")]
    public async Task<ActionResult> DeprecateReleaseAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.DeprecateReleaseAsync(identity, scriptId, releaseId, cancellationToken));
    }

    [HttpPost("{scriptId}/releases/{releaseId}/restore")]
    public async Task<ActionResult> RestoreReleaseAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.RestoreReleaseAsync(identity, scriptId, releaseId, cancellationToken));
    }

    [HttpDelete("{scriptId}/releases/{releaseId}")]
    public async Task<ActionResult> DeleteReleaseAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        return ToOperationResult(await _administrator.DeleteReleaseAsync(identity, scriptId, releaseId, cancellationToken));
    }
}
