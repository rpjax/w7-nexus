namespace Nexus.Actors.Requests;

public class AssignGatewayAccountToTeamRequest
{
    public string? TeamId { get; set; }
    public string? GatewayCredentialsId { get; set; }
}
