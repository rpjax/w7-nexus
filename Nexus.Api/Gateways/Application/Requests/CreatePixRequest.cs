namespace Nexus.Gateways.Application.Requests;

public sealed class CreatePixRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
