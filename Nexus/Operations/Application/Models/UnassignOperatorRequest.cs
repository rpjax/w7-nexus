namespace Nexus.Operations.Application.Models;

public class UnassignOperatorRequest
{
    public string OperationId { get; set; } = default!;
    public string OperatorId { get; set; } = default!;
}
