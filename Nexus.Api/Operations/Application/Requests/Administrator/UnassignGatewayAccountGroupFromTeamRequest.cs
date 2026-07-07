namespace Nexus.Operations.Application.Requests.Administrator;

public class UnassignGatewayAccountGroupFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}
