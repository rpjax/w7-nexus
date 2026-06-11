namespace Nexus.Actors.Requests;

public class AssignGatewayAccountToTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsId { get; set; } = string.Empty;
}
