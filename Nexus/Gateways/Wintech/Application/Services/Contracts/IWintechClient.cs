using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Services.Contracts;

namespace Nexus.Gateways.Wintech.Application.Services.Contracts;

public interface IWintechClient
{
    Task<WintechPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        WintechPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
