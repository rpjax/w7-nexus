using Microsoft.AspNetCore.Mvc;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Requests;
using Nexus.Controllers;

namespace Nexus.Charges.Presentation;

[Route("api/charges")]
public sealed class ChargesController : NexusController
{
    private IChargeService _chargeService { get; }

    public ChargesController(IChargeService chargeService)
    {
        _chargeService = chargeService;
    }
    
    [HttpPost("pix")]
    public async Task<ActionResult> CreatePixChargeAsync(
        [FromBody] CreatePixChargeRequest request,
        CancellationToken cancellationToken) =>
        ToResponse(await _chargeService.CreatePixChargeAsync(request, cancellationToken));
}