using Aidan.Core.Patterns;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministratorTeamLeaderCandidateSearchService
{
    Task<IResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        SearchTeamLeaderCandidatesRequest request);
}
