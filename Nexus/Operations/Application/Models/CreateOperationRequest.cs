namespace Nexus.Operations.Application.Models;

public sealed class CreateOperationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public IEnumerable<string>? Operators { get; set; }
}
