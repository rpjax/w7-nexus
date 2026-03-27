using Nexus.Frendz.Application.Models;

namespace Nexus.Frendz.Application
{
    public interface IFrendzClient
    {
        Task<FrendzPixPaymentResult> CreatePixPaymentAsync(string apiToken, FrendzPixPaymentRequest request, CancellationToken cancellationToken = default);
    }
}