using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;
using Nexus.Payments.Application.Models;

namespace Nexus.Operators.Application.Contracts;

public interface IOperator
{
    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);
}
