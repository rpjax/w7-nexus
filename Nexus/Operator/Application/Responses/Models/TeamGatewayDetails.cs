namespace Nexus.Operator.Application.Responses.Models;

public class TeamGatewayCredentialDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Gateway { get; init; } = default!;
}

public class TeamGatewayGroupDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public int CredentialCount { get; init; }
}
