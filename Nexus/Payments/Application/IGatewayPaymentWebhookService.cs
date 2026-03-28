using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application;

/// <summary>Processa notificações de gateway e aplica transições via <see cref="IPaymentService"/>.</summary>
public interface IGatewayPaymentWebhookService
{
    Task ProcessFrendzPostbackAsync(string jsonBody, CancellationToken cancellationToken = default);

    Task ProcessStandardGatewayWebhookAsync(
        PaymentGateway gateway,
        string jsonBody,
        CancellationToken cancellationToken = default);
}
