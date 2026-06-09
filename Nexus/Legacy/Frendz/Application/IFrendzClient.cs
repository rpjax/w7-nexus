using Nexus.Legacy.Frendz.Application.Models;

namespace Nexus.Legacy.Frendz.Application
{
    public interface IFrendzClient
    {
        Task<FrendzPixPaymentResult> CreatePixPaymentAsync(string apiToken, FrendzPixPaymentRequest request, CancellationToken cancellationToken = default);
    }
}