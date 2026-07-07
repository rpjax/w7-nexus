using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;

namespace Nexus.Operations.Application.Contracts;

public interface IAdministratorOperatorAssignmentSearchService
{
    Task<IResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        SearchOperatorsToAssignRequest request);
}
