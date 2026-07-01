using Aidan.Core.Patterns;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Contracts;

public interface IAdSpoofQueryService
{
    Task<IResult<ListSpoofedAdsResponse>> ListAllAsync(CancellationToken cancellationToken = default);
}
