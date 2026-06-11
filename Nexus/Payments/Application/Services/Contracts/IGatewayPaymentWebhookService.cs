using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Services.Contracts;

namespace Nexus.Payments.Application.Services.Contracts;

/// <summary>Processa notificações de gateway e aplica transições via <see cref="IPaymentService"/>.</summary>
public interface IGatewayPaymentWebhookService
{
    Task ProcessFrendzPostbackAsync(string jsonBody, CancellationToken cancellationToken = default);

    Task ProcessStandardGatewayWebhookAsync(
        PaymentGateway gateway,
        string jsonBody,
        CancellationToken cancellationToken = default);
}
