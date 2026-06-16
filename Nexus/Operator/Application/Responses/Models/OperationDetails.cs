namespace Nexus.Operator.Application.Responses.Models;

public class OperationDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OperationAdministratorDetails[] Administrators { get; init; } = Array.Empty<OperationAdministratorDetails>();
    public TeamDetails[] Teams { get; init; } = Array.Empty<TeamDetails>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
