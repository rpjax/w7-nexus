using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Legacy.Payments.Application;

namespace Nexus.Legacy.Presentation;

/// <summary>Dispara processamento de webhook fora do ciclo da requisição HTTP (escopo próprio).</summary>
public static class GatewayWebhookBackground
{
    public static void Enqueue(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string jsonBody,
        Func<IGatewayPaymentWebhookService, string, CancellationToken, Task> processor)
    {
        var json = jsonBody;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IGatewayPaymentWebhookService>();
                await processor(svc, json, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gateway webhook background task failed.");
            }
        });
    }
}
