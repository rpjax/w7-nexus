namespace Nexus.Operations.Application.Models;

public class UnassignGatewayCredentialsRequest
{
    public string OperationId { get; set; } = default!;
    public string GatewayCredentialsId { get; set; } = default!;
}
