namespace Nexus.Operations.Application.Models;

public class AssignGatewayCredentialsRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string GatewayCredentialsId { get; set; } = string.Empty;
}
