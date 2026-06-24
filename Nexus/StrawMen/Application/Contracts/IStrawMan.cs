using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Models;

namespace Nexus.StrawMen.Application.Contracts;

public interface IStrawMan
{
    Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<StrawManSettingsDetails>> GetSettingsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default);
}
