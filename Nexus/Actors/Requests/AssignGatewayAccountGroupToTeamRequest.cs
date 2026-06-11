namespace Nexus.Actors.Requests;

public class AssignGatewayAccountGroupToTeamRequest
{
    public string? TeamId { get; set; }
    public string? GatewayCredentialsGroupId { get; set; }
}
