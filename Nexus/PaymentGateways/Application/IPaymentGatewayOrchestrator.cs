using Nexus.PaymentGateways.Application.Models;

namespace Nexus.PaymentGateways.Application;

public interface IPaymentGatewayOrchestrator
{
    Task<PixPayment> CreatePixPaymentAsync(CreateGatewayPixPaymentRequest request);
}
