using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Contracts;

public interface IAdPatchQueryService
{
    Task<IResult<ListPatchedAdsResponse>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<AdPatch?> FindByOperationAndAdAsync(
        string operationId,
        string adId,
        CancellationToken cancellationToken = default);
}
