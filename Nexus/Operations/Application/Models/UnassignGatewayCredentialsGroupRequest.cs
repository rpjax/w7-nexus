namespace Nexus.Operations.Application.Models;

public class UnassignGatewayCredentialsGroupRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}
