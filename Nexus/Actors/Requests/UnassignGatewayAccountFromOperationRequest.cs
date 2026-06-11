namespace Nexus.Actors.Requests;

public class UnassignGatewayAccountFromOperationRequest
{
    public string? OperationId { get; set; }
    public string? GatewayCredentialsId { get; set; }
}
