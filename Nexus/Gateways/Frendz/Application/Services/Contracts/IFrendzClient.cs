using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Services.Contracts;

namespace Nexus.Gateways.Frendz.Application.Services.Contracts;

public interface IFrendzClient
{
    Task<FrendzPixPaymentResult> CreatePixPaymentAsync(string apiToken, FrendzPixPaymentRequest request, CancellationToken cancellationToken = default);
}
