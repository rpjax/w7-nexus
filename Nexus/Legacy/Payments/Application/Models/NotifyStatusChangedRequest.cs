using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Legacy.Payments.Application.Models;

public class NotifyStatusChangedRequest
{
    public string PaymentId { get; init; }
    public PaymentStatus Status { get; init; }

    public NotifyStatusChangedRequest(
        string paymentId,
        PaymentStatus status)
    {
        PaymentId = paymentId;
        Status = status;
    }
}
