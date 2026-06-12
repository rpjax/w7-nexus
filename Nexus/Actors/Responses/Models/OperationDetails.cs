namespace Nexus.Actors.Responses.Models;

public class OperationDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string[] AdministratorIds { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
