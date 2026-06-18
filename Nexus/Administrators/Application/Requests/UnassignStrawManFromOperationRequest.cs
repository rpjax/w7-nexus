namespace Nexus.Administrators.Application.Requests;

public class UnassignStrawManFromOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}
