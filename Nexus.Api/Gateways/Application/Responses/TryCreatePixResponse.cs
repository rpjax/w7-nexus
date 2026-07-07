using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application.Responses;

public sealed class TryCreatePixResponse
{
    public string TransactionId { get; init; } = string.Empty;
    public string PixCode { get; init; } = string.Empty;
    public PaymentGateway Gateway { get; init; }
    public string CredentialId { get; init; } = string.Empty;
}
