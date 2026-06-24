namespace Nexus.Payments.Application.Models;

public sealed class PaymentDetails
{
    public string Id { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public string? OperatorId { get; init; }
    public string StrawManId { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
    public string GatewayTransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public IReadOnlyList<PaymentSplitDetails> Splits { get; init; } = Array.Empty<PaymentSplitDetails>();
    public string Status { get; init; } = string.Empty;
    public string SettlementStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime? RefundedAt { get; init; }
    public DateTime? KilledAt { get; init; }
    public string? KillReason { get; init; }
    public DateTime? WithdrawnAt { get; init; }
}
