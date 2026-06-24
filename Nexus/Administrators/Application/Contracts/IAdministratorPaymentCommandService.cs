using Aidan.Core.Patterns;
using Nexus.Payments.Application.Models;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorPaymentCommandService
{
    Task<IResult<PaymentDetails>> PayAndGetAsync(string paymentId);
    Task<IResult<PaymentDetails>> RefundAndGetAsync(string paymentId);
    Task<IResult<PaymentDetails>> KillAndGetAsync(string paymentId, string reason);
    Task<IResult> DeletePaymentAsync(string paymentId);
    Task<IResult<PaymentDetails>> BindOperatorAsync(string paymentId, string operatorAccountId);
    Task<IResult<PaymentDetails>> BindStrawManAsync(string paymentId, string strawManAccountId);
}
