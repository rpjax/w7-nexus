using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;
using Nexus.Payments.Application.Models;

namespace Nexus.Operators.Application.Services;

public class Operator : IOperator
{
    private IOperatorAccessPolicy _policy { get; }
    private IOperatorOperationSearchService _operationSearch { get; }
    private IOperatorPaymentSearchService _paymentSearch { get; }

    public Operator(
        IOperatorAccessPolicy policy,
        IOperatorOperationSearchService operationSearch,
        IOperatorPaymentSearchService paymentSearch)
    {
        _policy = policy;
        _operationSearch = operationSearch;
        _paymentSearch = paymentSearch;
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

    public Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _paymentSearch.SearchPaymentsAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _paymentSearch.GetPaymentAsync(identity, paymentId),
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
