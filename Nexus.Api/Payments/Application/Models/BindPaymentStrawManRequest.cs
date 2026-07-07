using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Models;

public sealed class BindPaymentStrawManRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public IReadOnlyList<PaymentSplit>? Splits { get; init; }
}
