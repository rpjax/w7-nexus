using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Gateways.Application.Models;
using Nexus.Payments.Presentation;

namespace Nexus.Gateways.SigiloPay.Presentation;

[Route("api/sigilopay")]
public class SigiloPayController : ControllerBase
{
    private IServiceScopeFactory _scopeFactory { get; }
    private ILogger<SigiloPayController> _logger { get; }

    public SigiloPayController(
        IServiceScopeFactory scopeFactory,
        ILogger<SigiloPayController> logger)
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
            (svc, json, ct) => svc.ProcessStandardGatewayWebhookAsync(PaymentGateway.SigiloPay, json, ct));

        return Ok();
    }
}
