using Microsoft.AspNetCore.Mvc;
using Nexus.Controllers;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests.Victim;

namespace Nexus.Olx.Presentation;

[Route("api/olx/victim")]
public sealed class VictimController : NexusController
{
    private readonly IVictim _victim;

    public VictimController(IVictim victim)
    {
        _victim = victim;
    }

    [HttpGet("ad-patches")]
    public async Task<ActionResult> ListAdPatchesAsync(CancellationToken cancellationToken) =>
        ToResponse(await _victim.ListAdPatchesAsync(cancellationToken));

    [HttpPost("pix-payment")]
    public async Task<ActionResult> CreatePixPaymentAsync(
        [FromBody] CreatePixPaymentRequest request,
        CancellationToken cancellationToken) =>
        ToResponse(await _victim.CreatePixPaymentAsync(request, cancellationToken));
}
