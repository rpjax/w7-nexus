using Aidan.Core.Patterns;
using Nexus.Olx.Application.Requests.Victim;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Contracts;

public interface IVictim
{
    Task<IResult<ListPatchedAdsResponse>> ListAdPatchesAsync(CancellationToken cancellationToken = default);

    Task<IResult<CreatePixPaymentResponse>> CreatePixPaymentAsync(
        CreatePixPaymentRequest request,
        CancellationToken cancellationToken = default);
}
