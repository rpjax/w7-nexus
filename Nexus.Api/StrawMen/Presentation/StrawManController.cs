using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Presentation;

[Route("api/straw-men/straw-man")]
[Authorize]
public sealed class StrawManController : NexusController
{
    private IStrawMan _strawMan { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public StrawManController(
        IStrawMan strawMan,
        IRequesterIdentityResolver identityResolver)
    {
        _strawMan = strawMan;
        _identityResolver = identityResolver;
    }

    [HttpGet("settings")]
    public async Task<ActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _strawMan.GetSettingsAsync(
            identity,
            cancellationToken));
    }
}
