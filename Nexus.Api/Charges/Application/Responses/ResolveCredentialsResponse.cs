using Nexus.Gateways.Application.Models;

namespace Nexus.Charges.Application.Responses;

public sealed class ResolveCredentialsResponse
{
    public GatewayCredentialReference[] Credentials { get; init; } = Array.Empty<GatewayCredentialReference>();
    public IReadOnlyDictionary<string, string> StrawManIdByCredentialId { get; init; }
        = new Dictionary<string, string>();
}
