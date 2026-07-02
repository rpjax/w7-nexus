using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.OperationAdministrator;
using Nexus.Operations.Application.Responses.OperationAdministrator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationAdministratorTeamLeaderCandidateSearchService
{
    Task<IResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        SearchTeamLeaderCandidatesRequest request);
}
