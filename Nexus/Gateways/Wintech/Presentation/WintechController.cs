using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Gateways.Application.Models;
using Nexus.Payments.Presentation;

namespace Nexus.Gateways.Wintech.Presentation;

[Route("api/wintech")]
public class WintechController : ControllerBase
{
    private IServiceScopeFactory _scopeFactory { get; }
    private ILogger<WintechController> _logger { get; }

    public WintechController(
        IServiceScopeFactory scopeFactory,
        ILogger<WintechController> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("webhook/callback")]
    public async Task<IActionResult> WebhookCallbackAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
            raw = "{}";

        GatewayWebhookBackground.Enqueue(
            _scopeFactory,
            _logger,
            raw,
            (svc, json, ct) => svc.ProcessStandardGatewayWebhookAsync(PaymentGateway.Wintech, json, ct));

        return Ok();
    }
}
