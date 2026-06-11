using Aidan.Web.Controllers;
using Nexus.Gateways.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Presentation;

[ApiController]
[Route("api/gateways")]
public sealed class GatewaysController : WebController
{
    private IGatewayOrchestrator _gatewayOrchestrator { get; }

    public GatewaysController(IGatewayOrchestrator gatewayOrchestrator)
    {
        _gatewayOrchestrator = gatewayOrchestrator;
    }

    [HttpPost("pix")]
    public async Task<IActionResult> CreateGatewayPixAsync([FromBody] CreateGatewayPixRequest request)
    {
        var result = await _gatewayOrchestrator.CreateGatewayPixAsync(request);

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
