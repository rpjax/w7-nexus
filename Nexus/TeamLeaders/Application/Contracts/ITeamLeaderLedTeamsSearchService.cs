using Aidan.Core.Patterns;
using Nexus.Authorizations.Application.Models;
using Nexus.TeamLeaders.Application.Requests;
using Nexus.TeamLeaders.Application.Responses;

namespace Nexus.TeamLeaders.Application.Contracts;

public interface ITeamLeaderLedTeamsSearchService
{
    Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request);
}
