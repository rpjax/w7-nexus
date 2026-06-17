using Nexus.Authorizations.Application.Models;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;

namespace Nexus.Operators.Application.Contracts;

public interface IOperator
{
    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);
}
