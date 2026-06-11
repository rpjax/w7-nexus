namespace Nexus.Actors.Requests;

public class UnassignGatewayAccountGroupFromOperationRequest
{
    public string? OperationId { get; set; }
    public string? GatewayCredentialsGroupId { get; set; }
}
