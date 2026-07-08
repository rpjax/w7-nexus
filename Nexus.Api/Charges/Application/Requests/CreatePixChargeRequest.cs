namespace Nexus.Charges.Application.Models;

public sealed class CreatePixChargeRequest
{
    public string OperationId { get; init; } = string.Empty;
    public string? OperatorId { get; init; }
    public decimal Amount { get; init; }
}
