namespace Nexus.Administrators.Application.Responses.Models;

public class OperationDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OperationAdministratorDetails[] Administrators { get; init; } = Array.Empty<OperationAdministratorDetails>();
    public string GatewaySelectionStrategy { get; init; } = default!;
    public TeamAccountDetails[] StrawMen { get; init; } = Array.Empty<TeamAccountDetails>();
    public TeamGatewayCredentialDetails[] GatewayCredentials { get; init; } = Array.Empty<TeamGatewayCredentialDetails>();
    public TeamGatewayGroupDetails[] GatewayCredentialsGroups { get; init; } = Array.Empty<TeamGatewayGroupDetails>();
    public TeamDetails[] Teams { get; init; } = Array.Empty<TeamDetails>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class OperationAdministratorDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
}
