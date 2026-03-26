using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;

namespace Nexus.Tests.PaymentGateways;

internal sealed class StubPaymentGatewayService : IPaymentGatewayService
{
    public Func<string, decimal, Task<PixPayment>>? OnCreate { get; init; }

    public Task<PixPayment> CreatePixPaymentAsync(string userId, decimal amount)
    {
        if (OnCreate is null)
            throw new InvalidOperationException("OnCreate is not configured.");

        return OnCreate(userId, amount);
    }
}
