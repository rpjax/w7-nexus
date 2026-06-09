using Aidan.Core.Patterns;
using Nexus.Legacy.Payments.Aggregates;
using Nexus.Legacy.Payments.Application.Models;

namespace Nexus.Legacy.Payments.Application;

public interface IPaymentService
{
    Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request);
    Task<IResult> DeletePaymentAsync(string paymentId);
    Task<IResult> PayAsync(string paymentId);
    Task<IResult> RefundAsync(string paymentId);
    Task<IResult> KillAsync(string paymentId, string reason);
}
