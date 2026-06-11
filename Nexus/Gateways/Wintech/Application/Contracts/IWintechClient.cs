using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Contracts;

namespace Nexus.Gateways.Wintech.Application.Contracts;

public interface IWintechClient
{
    Task<WintechPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        WintechPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
