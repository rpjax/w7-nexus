using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Application;

public interface ISigiloPayClient
{
    Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        SigiloPayPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
