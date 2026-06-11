namespace Nexus.Actors.Requests;

public class UnassignGatewayAccountGroupFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}
