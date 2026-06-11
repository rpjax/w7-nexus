namespace Nexus.Actors.Requests;

public class AssignGatewayAccountToOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string GatewayCredentialsId { get; set; } = string.Empty;
}
