using Nexus.Actors.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Actors.Extensions;

public static class OperationExtensions
{
    public static OperationDetails ToOperationDetails(this Operation operation)
    {
        return new OperationDetails
        {
            Id = operation.Id,
            Name = operation.Name,
            Description = operation.Description
        };
    }
}
