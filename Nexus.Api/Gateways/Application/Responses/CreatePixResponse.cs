namespace Nexus.Gateways.Application.Responses;

public sealed class CreatePixResponse
{
    public string TransactionId { get; init; } = string.Empty;
    public string PixCode { get; init; } = string.Empty;
}
