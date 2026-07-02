using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> PayPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> RefundPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> KillPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> MarkPaymentAsDistributedAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeletePaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> BindPaymentOperatorAsync(
        RequesterIdentity identity,
        string paymentId,
        string operatorId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> BindPaymentStrawManAsync(
        RequesterIdentity identity,
        string paymentId,
        string strawManId,
        CancellationToken cancellationToken = default);
}
