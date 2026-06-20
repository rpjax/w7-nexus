using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorOperationPickerSearchService
{
    Task<IResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        SearchOperationsToAssignRequest request);
}
