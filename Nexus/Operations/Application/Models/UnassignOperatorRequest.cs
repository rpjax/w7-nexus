namespace Nexus.Operations.Application.Models;

public class UnassignOperatorRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
}
