namespace Nexus.Operations.Application.Requests.Administrator;

public class AssignGatewayAccountToTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsId { get; set; } = string.Empty;
}
