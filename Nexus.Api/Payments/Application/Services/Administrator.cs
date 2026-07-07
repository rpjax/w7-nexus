using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IAdministratorPaymentSearchService _paymentSearch;
    private readonly IAdministratorPaymentCommandService _paymentCommands;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorPaymentSearchService paymentSearch,
        IAdministratorPaymentCommandService paymentCommands)
    {
        _policy = policy;
        _paymentSearch = paymentSearch;
        _paymentCommands = paymentCommands;
    }

    public Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentSearch.SearchPaymentsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentSearch.GetPaymentAsync(paymentId), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> PayPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentCommands.PayAndGetAsync(paymentId), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> RefundPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentCommands.RefundAndGetAsync(paymentId), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> KillPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentCommands.KillAndGetAsync(paymentId, reason), cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> MarkPaymentAsDistributedAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _paymentCommands.MarkAsDistributedAndGetAsync(paymentId), cancellationToken);
    }

    public async Task<IOperationResult<bool>> DeletePaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<bool>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<bool>.Unauthorized(authorization.AuthorizationErrors);

        var deleteResult = await _paymentCommands.DeletePaymentAsync(paymentId);
        if (deleteResult.IsFailure)
            return OperationResult<bool>.Failure(deleteResult.Errors);

        return OperationResult<bool>.Success(true);
    }

    public Task<IOperationResult<PaymentDetails>> BindPaymentOperatorAsync(
        RequesterIdentity identity,
        string paymentId,
        string operatorId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _paymentCommands.BindOperatorAsync(paymentId, operatorId),
            cancellationToken);
    }

    public Task<IOperationResult<PaymentDetails>> BindPaymentStrawManAsync(
        RequesterIdentity identity,
        string paymentId,
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _paymentCommands.BindStrawManAsync(paymentId, strawManId),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

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
