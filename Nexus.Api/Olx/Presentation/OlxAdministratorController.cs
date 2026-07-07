using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Administrator;

namespace Nexus.Olx.Presentation;

[Route("api/olx/admin")]
[Authorize]
public sealed class OlxAdministratorController : NexusController
{
    private readonly IOlxAdministrator _olxAdministrator;
    private readonly IRequesterIdentityResolver _identityResolver;

    public OlxAdministratorController(
        IOlxAdministrator olxAdministrator,
        IRequesterIdentityResolver identityResolver)
    {
        _olxAdministrator = olxAdministrator;
        _identityResolver = identityResolver;
    }

    [HttpPost("ad-patches/search")]
    public async Task<ActionResult> SearchAdPatchesAsync(
        [FromBody] SearchAdPatchesRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxAdministrator.SearchAdPatchesAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("ads/unimpersonate")]
    public async Task<ActionResult> UnimpersonateAdAsync(
        [FromBody] UnimpersonateAdRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxAdministrator.UnimpersonateAdAsync(
            identity,
            request,
            cancellationToken));
    }
}
