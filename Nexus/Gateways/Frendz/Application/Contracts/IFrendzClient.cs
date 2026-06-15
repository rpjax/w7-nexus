using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;

namespace Nexus.Gateways.Frendz.Application.Contracts;

public interface IFrendzClient
{
    Task<FrendzPixPaymentResult> CreatePixPaymentAsync(string apiToken, FrendzPixPaymentRequest request, CancellationToken cancellationToken = default);
}
