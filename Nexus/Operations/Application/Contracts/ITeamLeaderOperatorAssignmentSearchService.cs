using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamLeaderOperatorAssignmentSearchService
{
    Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request);
}
