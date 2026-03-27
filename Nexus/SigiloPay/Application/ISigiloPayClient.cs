using Nexus.SigiloPay.Application.Models;

namespace Nexus.SigiloPay.Application;

public interface ISigiloPayClient
{
    Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        SigiloPayPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
