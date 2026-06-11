using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application
{
    public interface IFrendzClient
    {
        Task<FrendzPixPaymentResult> CreatePixPaymentAsync(string apiToken, FrendzPixPaymentRequest request, CancellationToken cancellationToken = default);
    }
}