using Nexus.Authorization.Application.Models;
using Nexus.Operator.Application.Requests;
using Nexus.Operator.Application.Responses;

namespace Nexus.Operator.Application.Contracts;

public interface IOperator
{
    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);
}
