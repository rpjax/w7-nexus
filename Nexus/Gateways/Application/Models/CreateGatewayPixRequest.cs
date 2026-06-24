namespace Nexus.Gateways.Application.Models;

public sealed class CreateGatewayPixRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public string? StrawManId { get; init; }
    public string? OperatorId { get; init; }
    public decimal Amount { get; init; }
}
