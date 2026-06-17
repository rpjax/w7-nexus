namespace Nexus.Administrator.Application.Requests;

public class UnassignGatewayAccountFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsId { get; set; } = string.Empty;
}
