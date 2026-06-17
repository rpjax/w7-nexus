using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeaderLedTeamsSearchService
{
    Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request);
}
