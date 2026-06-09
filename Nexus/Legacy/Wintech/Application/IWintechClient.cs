using Nexus.Legacy.Wintech.Application.Models;

namespace Nexus.Legacy.Wintech.Application;

public interface IWintechClient
{
    Task<WintechPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        WintechPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
