using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;

namespace Nexus.Gateways.SigiloPay.Application.Contracts;

public interface ISigiloPayClient
{
    Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        SigiloPayPixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
