using Nexus.Legacy.SigiloPay.Application.Models;

namespace Nexus.Legacy.SigiloPay.Application;

public interface ISigiloPayClient
{
    Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        SigiloPayPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
