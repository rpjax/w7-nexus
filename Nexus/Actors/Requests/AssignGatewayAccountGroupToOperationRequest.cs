namespace Nexus.Actors.Requests;

public class AssignGatewayAccountGroupToOperationRequest
{
    public string? OperationId { get; set; }
    public string? GatewayCredentialsGroupId { get; set; }
}
