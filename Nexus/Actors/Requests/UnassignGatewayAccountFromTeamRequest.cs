namespace Nexus.Actors.Requests;

public class UnassignGatewayAccountFromTeamRequest
{
    public string? TeamId { get; set; }
    public string? GatewayCredentialsId { get; set; }
}
