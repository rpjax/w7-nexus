using Aidan.Core.Patterns;
using Nexus.TeamLeaders.Application.Requests;
using Nexus.TeamLeaders.Application.Responses;

namespace Nexus.TeamLeaders.Application.Contracts;

public interface ITeamLeaderOperatorAssignmentSearchService
{
    Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request);
}
