namespace Nexus.Operations.Application.Models;

public class UnassignGatewayCredentialsGroupRequest
{
    public string OperationId { get; set; } = default!;
    public string GatewayCredentialsGroupId { get; set; } = default!;
}
