using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface IOperationAdministratorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}
