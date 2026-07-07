using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Requests.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamLeaderLedTeamsSearchService
{
    Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request);
}
