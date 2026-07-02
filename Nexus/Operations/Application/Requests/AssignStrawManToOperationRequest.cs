namespace Nexus.Operations.Application.Requests.Administrator;

public class AssignStrawManToOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}
