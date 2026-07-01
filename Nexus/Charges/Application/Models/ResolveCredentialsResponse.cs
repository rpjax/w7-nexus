using Nexus.Gateways.Application.Models;

namespace Nexus.Charges.Application.Models;

public sealed class ResolveCredentialsResponse
{
    public GatewayCredentialReference[] Credentials { get; init; } = [];
    public IReadOnlyDictionary<string, string> StrawManIdByCredentialId { get; init; }
        = new Dictionary<string, string>();
}
