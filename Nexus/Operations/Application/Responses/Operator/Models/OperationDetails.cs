namespace Nexus.Operations.Application.Responses.Operator.Models;

public class OperationDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OperationAdministratorDetails[] Administrators { get; init; } = Array.Empty<OperationAdministratorDetails>();
    public TeamDetails Team { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
