using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Services;

public sealed class Operator : IOperator
{
    private readonly IOperatorAccessPolicy _policy;
    private readonly IOperatorPaymentSearchService _paymentSearch;

    public Operator(
        IOperatorAccessPolicy policy,
        IOperatorPaymentSearchService paymentSearch)
    {
        _policy = policy;
        _paymentSearch = paymentSearch;
    }

    public Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentSearch.SearchPaymentsAsync(identity, request), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentSearch.GetPaymentAsync(identity, paymentId), cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeOperatorAsync(identity);

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
