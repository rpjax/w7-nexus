using Aidan.Core.Patterns;
using Nexus.Payments.Application.Models;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorPaymentSearchService
{
    Task<IResult<SearchPaymentsResponse>> SearchPaymentsAsync(SearchPaymentsRequest request);
    Task<IResult<PaymentDetails>> GetPaymentAsync(string paymentId);
}
