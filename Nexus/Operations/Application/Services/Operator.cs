using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.Operator;
using Nexus.Operations.Application.Responses.Operator;

namespace Nexus.Operations.Application.Services;

public sealed class Operator : IOperator
{
    private readonly IOperatorAccessPolicy _policy;
    private readonly IOperatorOperationSearchService _operationSearch;

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
            identity,
            () => _operationSearch.SearchOperationsAsync(identity, request),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeSearchOperationsAsync(identity, cancellationToken);

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
