namespace Nexus.Actors.Requests;

public class UnassignGatewayAccountGroupFromTeamRequest
{
    public string? TeamId { get; set; }
    public string? GatewayCredentialsGroupId { get; set; }
}
