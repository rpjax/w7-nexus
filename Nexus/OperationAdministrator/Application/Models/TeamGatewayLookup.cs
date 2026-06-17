using Nexus.OperationAdministrator.Application.Responses.Models;

namespace Nexus.OperationAdministrator.Application.Models;

public sealed class TeamGatewayLookup
{
    public IReadOnlyDictionary<string, TeamGatewayCredentialDetails> CredentialsById { get; init; }
        = new Dictionary<string, TeamGatewayCredentialDetails>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TeamGatewayGroupDetails> GroupsById { get; init; }
        = new Dictionary<string, TeamGatewayGroupDetails>(StringComparer.Ordinal);
}
