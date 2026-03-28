using Nexus.Wintech.Application.Models;

namespace Nexus.Wintech.Application;

public interface IWintechClient
{
    Task<WintechPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        WintechPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
