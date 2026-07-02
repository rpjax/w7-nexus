namespace Nexus.Operations.Application.Responses.OperationAdministrator.Models;

public class TeamGatewayGroupDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public int CredentialCount { get; init; }
}
