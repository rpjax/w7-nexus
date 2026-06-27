using Aidan.Core.Patterns;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Contracts;

public interface IVictim
{
    Task<IResult<ListSpoofedAdsResponse>> ListAdSpoofsAsync(CancellationToken cancellationToken = default);
}
