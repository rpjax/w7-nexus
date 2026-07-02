using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Requests.Operator;
using Nexus.Operations.Application.Responses.Operator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperator
{
    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);
}
