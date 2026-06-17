using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(SearchOperationsRequest request);
}
