using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application.Requests;

public sealed class TryCreatePixRequest
{
    public string PaymentId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public IReadOnlyList<GatewayCredentialReference> Credentials { get; init; } = [];
}
