using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Payments.Presentation;

namespace Nexus.Gateways.Frendz.Presentation;

[Route("api/frendz")]
public class FrendzController : ControllerBase
{
    private IServiceScopeFactory _scopeFactory { get; }
    private ILogger<FrendzController> _logger { get; }

    public FrendzController(
        IServiceScopeFactory scopeFactory,
        ILogger<FrendzController> logger)
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
            (svc, json, ct) => svc.ProcessFrendzPostbackAsync(json, ct));

        return Ok();
    }
}
