namespace Nexus.Operations.Aggregates;

public interface IGatewayCredentialScope
{
    GatewaySelectionStrategy GatewaySelectionStrategy { get; }
    IReadOnlyList<string> StrawManIds { get; }
    IReadOnlyList<string> GatewayCredentialsIds { get; }
    IReadOnlyList<string> GatewayCredentialsGroupIds { get; }
}
