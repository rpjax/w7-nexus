using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Models;

namespace Nexus.Operators.Application.Contracts;

public interface IOperatorPaymentSearchService
{
    Task<IResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request);

    Task<IResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId);
}
