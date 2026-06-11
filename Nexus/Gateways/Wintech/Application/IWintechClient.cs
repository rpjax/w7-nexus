using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Wintech.Application;

public interface IWintechClient
{
    Task<WintechPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        WintechPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
