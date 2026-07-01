using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Presentation;

[ApiController]
[Route("api/charges")]
public sealed class ChargesController : WebController
{
    private IChargeService _chargeService { get; }

    public ChargesController(IChargeService chargeService)
    {
        _chargeService = chargeService;
    }

    [HttpPost("pix")]
    public async Task<IActionResult> CreatePixChargeAsync([FromBody] CreatePixChargeRequest request)
    {
        var result = await _chargeService.CreatePixChargeAsync(request);

        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        if (result.Value is null)
            throw new InvalidOperationException();

        return Ok(new
        {
            Id = result.Value.Id,
            PixCode = result.Value.PixCode,
            PaymentRecipient = result.Value.PaymentRecipient,
        });
    }
}
