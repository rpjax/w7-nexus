namespace Nexus.Legacy.Charges.Application.Models;

public sealed class CreatePixChargeRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public string? StrawManAccountId { get; init; }
    public string? OperatorAccountId { get; init; }
    public decimal Amount { get; init; }
}
