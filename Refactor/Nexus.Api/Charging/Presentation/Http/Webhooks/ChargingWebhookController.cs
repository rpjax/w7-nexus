using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Charging.Presentation.Http.Webhooks;

[Route("api/charging/webhooks")]
[AllowAnonymous]
public sealed class ChargingWebhookController : ApiControllerBase
{
    private readonly IMarkChargePaidUseCase _markPaid;
    private readonly IConfiguration _configuration;

    public ChargingWebhookController(IMarkChargePaidUseCase markPaid, IConfiguration configuration)
    {
        _markPaid = markPaid;
        _configuration = configuration;
    }

    [HttpPost("paid")]
    public async Task<ActionResult> PaidAsync([FromBody] MarkPaidWebhookRequest request, CancellationToken cancellationToken)
    {
        var expected = _configuration["Charging:WebhookSecret"] ?? "";
        var provided = Request.Headers["X-Nexus-Webhook-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expected)
            || !string.Equals(expected, provided, StringComparison.Ordinal))
        {
            return ProblemResponse(401, Aidan.Core.Errors.Error.Create()
                .WithCode(ChargingErrorCodes.WebhookUnauthorized)
                .WithMessage("Webhook secret invalido.")
                .Build());
        }

        return ToOperationResult(await _markPaid.HandleAsync(
            new MarkChargePaidCommand(request.ChargeId, request.ExternalReference),
            cancellationToken));
    }
}
