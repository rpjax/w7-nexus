using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Presentation;

[Route("api/straw-men/administrator")]
[Authorize]
public sealed class AdministratorController : NexusController
{
    private IAdministrator _administrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public AdministratorController(
        IAdministrator administrator,
        IRequesterIdentityResolver identityResolver)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
    }

    [HttpGet("{strawManId}/settings")]
    public async Task<ActionResult> GetStrawManSettingsAsync(
        string strawManId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GetStrawManSettingsAsync(
            identity,
            strawManId,
            cancellationToken));
    }

    [HttpPut("{strawManId}/settings")]
    public async Task<ActionResult> UpsertStrawManSettingsAsync(
        string strawManId,
        [FromBody] UpdateStrawManSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UpsertStrawManSettingsAsync(
            identity,
            strawManId,
            request?.MovementFeePercentage ?? 0m,
            cancellationToken));
    }
}
