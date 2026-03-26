using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;

namespace Nexus.Tests.PaymentGateways;

internal sealed class StubPaymentGatewayService : IPaymentGatewayService
{
    public Func<CreateGatewayPixPaymentRequest, Task<PixPayment>>? OnCreate { get; init; }

    public Task<PixPayment> CreatePixPaymentAsync(CreateGatewayPixPaymentRequest request)
    {
        if (OnCreate is null)
            throw new InvalidOperationException("OnCreate is not configured.");

        return OnCreate(request);
    }
}
