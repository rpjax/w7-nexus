using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Legacy.Payments.Application.Models;

public class PaymentStatusChangedNotification
{
    public string PaymentId { get; init; }
    public PaymentStatus Status { get; init; }

    public PaymentStatusChangedNotification(
        string paymentId,
        PaymentStatus status)
    {
        PaymentId = paymentId;
        Status = status;
    }
}