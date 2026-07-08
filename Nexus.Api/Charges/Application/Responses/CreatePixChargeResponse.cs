namespace Nexus.Charges.Application.Models;

public sealed class CreatePixChargeResponse
{
    public string Id { get; init; } = string.Empty;
    public string PixCode { get; init; } = string.Empty;
    public string PaymentRecipient { get; init; } = string.Empty;
    public int ExpirationTimeSeconds { get; init; }
}
