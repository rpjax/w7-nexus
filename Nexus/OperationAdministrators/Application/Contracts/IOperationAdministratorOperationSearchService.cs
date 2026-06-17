using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministratorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}
