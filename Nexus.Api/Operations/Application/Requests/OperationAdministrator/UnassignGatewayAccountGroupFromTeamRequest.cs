namespace Nexus.Operations.Application.Requests.OperationAdministrator;

public class UnassignGatewayAccountGroupFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}
