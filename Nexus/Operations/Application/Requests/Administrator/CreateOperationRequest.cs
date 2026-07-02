namespace Nexus.Operations.Application.Requests.Administrator;

public class CreateOperationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
