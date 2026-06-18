using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentService
{
    Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request);
    Task<IResult> DeletePaymentAsync(string paymentId);
    Task<IResult> PayAsync(string paymentId);
    Task<IResult> MarkAsWithdrawnAsync(string paymentId);
    Task<IResult> RefundAsync(string paymentId);
    Task<IResult> KillAsync(string paymentId, string reason);
}
