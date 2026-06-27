using Aidan.Core.Patterns;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Services;

public sealed class Victim : IVictim
{
    private readonly IAdSpoofQueryService _query;

    public Victim(IAdSpoofQueryService query)
    {
        _query = query;
    }

    public Task<IResult<ListSpoofedAdsResponse>> ListAdSpoofsAsync(CancellationToken cancellationToken = default) =>
        _query.ListAllAsync(cancellationToken);
}
