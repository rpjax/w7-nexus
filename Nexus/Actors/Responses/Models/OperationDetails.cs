using Nexus.Operations.Aggregates;

namespace Nexus.Actors.Responses.Models;

public class OperationDetails
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }

    public static OperationDetails FromOperation(Operation operation)
    {
        return new OperationDetails
        {
            Id = operation.Id,
            Name = operation.Name,
            Description = operation.Description
        };
    }
}
