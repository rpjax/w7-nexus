namespace Nexus.Administrator.Application.Requests;

public class CreateOperationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
