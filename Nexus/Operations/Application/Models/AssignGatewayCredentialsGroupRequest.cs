namespace Nexus.Operations.Application.Models;

public class AssignGatewayCredentialsGroupRequest
{
    public string OperationId { get; set; } = default!;
    public string GatewayCredentialsGroupId { get; set; } = default!;
}
