using Aidan.Core.Patterns;
using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application;

public interface IPixPaymentService
{
    Task<IResult<PixPayment>> CreatePixPaymentAsync(CreatePixPaymentRequest request);
    Task<IResult> PayAsync(string paymentId);
    Task<IResult> RefundAsync(string paymentId);
    Task<IResult> KillAsync(string paymentId, string reason);
}
