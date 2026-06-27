using Microsoft.AspNetCore.Mvc;
using Nexus.Controllers;
using Nexus.Olx.Application.Contracts;

namespace Nexus.Olx.Presentation;

[Route("api/olx/victim")]
public sealed class VictimController : NexusController
{
    private readonly IVictim _victim;

    public VictimController(IVictim victim)
    {
        _victim = victim;
    }

    [HttpGet("ad-spoofs")]
    public async Task<ActionResult> ListAdSpoofsAsync(CancellationToken cancellationToken) =>
        ToResponse(await _victim.ListAdSpoofsAsync(cancellationToken));
}
