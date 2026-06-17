using Aidan.Core.Patterns;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface IOperationAdministratorTeamLeaderCandidateSearchService
{
    Task<IResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        SearchTeamLeaderCandidatesRequest request);
}
