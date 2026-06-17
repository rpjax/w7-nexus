using Aidan.Core.Patterns;
using Nexus.Administrator.Application.Requests;
using Nexus.Administrator.Application.Responses;

namespace Nexus.Administrator.Application.Contracts;

public interface IAdministratorOperatorAssignmentSearchService
{
    Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request);
}
