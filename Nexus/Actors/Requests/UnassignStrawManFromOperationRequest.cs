namespace Nexus.Actors.Requests;

public class UnassignStrawManFromOperationRequest
{
    public string? OperationId { get; set; }
    public string? StrawManId { get; set; }
}
