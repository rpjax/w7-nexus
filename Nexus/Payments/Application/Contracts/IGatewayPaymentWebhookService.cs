using Nexus.Gateways.Application.Models;
using Nexus.Payments.Application.Contracts;

namespace Nexus.Payments.Application.Contracts;

/// <summary>Processa notificações de gateway e aplica transições via <see cref="IPaymentService"/>.</summary>
public interface IGatewayPaymentWebhookService
{
    Task ProcessFrendzPostbackAsync(string jsonBody, CancellationToken cancellationToken = default);

    Task ProcessStandardGatewayWebhookAsync(
        PaymentGateway gateway,
        string jsonBody,
        CancellationToken cancellationToken = default);
}
