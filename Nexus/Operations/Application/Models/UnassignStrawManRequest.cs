namespace Nexus.Operations.Application.Models;

public class UnassignStrawManRequest
{
    public string OperationId { get; set; } = default!;
    public string StrawManId { get; set; } = default!;
}
