using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operator.Application.Requests;
using Nexus.Operator.Application.Responses;

namespace Nexus.Operator.Application.Contracts;

public interface IOperatorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}
