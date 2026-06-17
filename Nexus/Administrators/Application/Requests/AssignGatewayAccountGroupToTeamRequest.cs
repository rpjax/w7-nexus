namespace Nexus.Administrators.Application.Requests;

public class AssignGatewayAccountGroupToTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}
