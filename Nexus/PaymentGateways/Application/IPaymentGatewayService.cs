using Nexus.PaymentGateways.Application.Models;

namespace Nexus.PaymentGateways.Application;

public interface IPaymentGatewayService
{
    Task<PixPayment> CreatePixPaymentAsync(CreateGatewayPixPaymentRequest request);
}
