using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Presentation;

[ApiController]
[Route("api/charges")]
public sealed class ChargesController : WebController
{
    private IChargeOrchestrator _chargeOrchestrator { get; }

    public ChargesController(IChargeOrchestrator chargeOrchestrator)
    {
        _chargeOrchestrator = chargeOrchestrator;
    }

    [HttpPost("pix")]
    public async Task<IActionResult> CreatePixChargeAsync([FromBody] CreatePixChargeRequest request)
    {
        var result = await _chargeOrchestrator.CreatePixChargeAsync(request);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        if(result.Value is null)
        {
            throw new InvalidOperationException();
        }

        return Ok(new
        {
            Id = result.Value.Id,
            Code = result.Value.Code,
        });
    }
}

