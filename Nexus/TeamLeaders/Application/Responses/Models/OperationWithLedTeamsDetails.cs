namespace Nexus.TeamLeaders.Application.Responses.Models;

public class OperationWithLedTeamsDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public TeamDetails[] Teams { get; init; } = Array.Empty<TeamDetails>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
