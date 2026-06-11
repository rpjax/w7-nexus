namespace Nexus.Actors.Requests;

public class CreateOperationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string[] Operators { get; set; } = new string[0];
}
