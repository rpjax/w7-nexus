namespace Nexus.Operations.Application.Models;

public class AssignGatewayCredentialsRequest
{
    public string OperationId { get; set; } = default!;
    public string GatewayCredentialsId { get; set; } = default!;
}
