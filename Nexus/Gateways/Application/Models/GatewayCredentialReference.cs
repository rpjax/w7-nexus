namespace Nexus.Gateways.Application.Models;

public sealed class GatewayCredentialReference
{
    public PaymentGateway Gateway { get; init; }
    public string CredentialId { get; init; } = string.Empty;
}
