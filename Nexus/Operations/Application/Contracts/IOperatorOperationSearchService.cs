using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Requests.Operator;
using Nexus.Operations.Application.Responses.Operator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperatorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}
