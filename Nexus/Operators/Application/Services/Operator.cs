using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;

namespace Nexus.Operators.Application.Services;

public class Operator : IOperator
{
    private IOperatorAccessPolicy _policy { get; }
    private IOperatorOperationSearchService _operationSearch { get; }

    public Operator(
        IOperatorAccessPolicy policy,
        IOperatorOperationSearchService operationSearch)
    {
        _policy = policy;
        _operationSearch = operationSearch;
    }

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _operationSearch.SearchOperationsAsync(identity, request),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<IAuthorizationResult>> authorizeAsync,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizeAsync(cancellationToken);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }
}
