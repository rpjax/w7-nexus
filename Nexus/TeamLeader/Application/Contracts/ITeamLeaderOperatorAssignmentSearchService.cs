using Aidan.Core.Patterns;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeaderOperatorAssignmentSearchService
{
    Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request);
}
