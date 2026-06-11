namespace Nexus.Actors.Requests;

public class UnassignOperatorFromOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
}
