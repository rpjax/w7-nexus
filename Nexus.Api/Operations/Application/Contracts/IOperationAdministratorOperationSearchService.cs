using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Requests.OperationAdministrator;
using Nexus.Operations.Application.Responses.OperationAdministrator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationAdministratorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}
