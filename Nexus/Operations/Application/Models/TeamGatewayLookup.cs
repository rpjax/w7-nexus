using Nexus.Operations.Application.Responses.Administrator.Models;

namespace Nexus.Operations.Application.Models;

public sealed class TeamGatewayLookup
{
    public IReadOnlyDictionary<string, TeamGatewayCredentialDetails> CredentialsById { get; init; }
        = new Dictionary<string, TeamGatewayCredentialDetails>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TeamGatewayGroupDetails> GroupsById { get; init; }
        = new Dictionary<string, TeamGatewayGroupDetails>(StringComparer.Ordinal);
}
