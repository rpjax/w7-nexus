namespace Nexus.Actors.Requests;

public class AssignGatewayAccountToOperationRequest
{
    public string? OperationId { get; set; }
    public string? GatewayCredentialsId { get; set; }
}
