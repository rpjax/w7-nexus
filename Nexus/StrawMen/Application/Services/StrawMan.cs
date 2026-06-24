using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Services;

public sealed class StrawMan : IStrawMan
{
    private IStrawManAccessPolicy _policy { get; }
    private IStrawManPaymentSearchService _paymentSearch { get; }
    private IStrawManSettingsQueryService _settingsQuery { get; }

    public StrawMan(
        IStrawManAccessPolicy policy,
        IStrawManPaymentSearchService paymentSearch,
        IStrawManSettingsQueryService settingsQuery)
    {
        _policy = policy;
        _paymentSearch = paymentSearch;
        _settingsQuery = settingsQuery;
    }

    public Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _paymentSearch.SearchPaymentsAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _paymentSearch.GetPaymentAsync(identity, paymentId),
            cancellationToken);
    }

    public Task<IOperationResult<StrawManSettingsDetails>> GetSettingsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _settingsQuery.GetSettingsAsync(identity.AccountId, cancellationToken),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeStrawManAsync(identity);

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
