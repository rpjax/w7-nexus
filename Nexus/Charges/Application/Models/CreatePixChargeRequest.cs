namespace Nexus.Charges.Application.Models;

public sealed class CreatePixChargeRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public string? OperatorAccountId { get; init; }
    public string? StrawManAccountId { get; init; }
    public decimal Amount { get; init; }
}
