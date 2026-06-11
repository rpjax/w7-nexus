namespace Nexus.Actors.Requests;

public class UnassignOperatorFromOperationRequest
{
    public string? OperationId { get; set; }
    public string? OperatorId { get; set; }
}
