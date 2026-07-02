using Nexus.Operations.Application.Responses.Administrator.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Mapping;

public static class OperationMapping
{
    public static OperationDetails ToOperationDetails(this Operation operation)
        => OperationDetailsMapper.Map(
            operation,
            Array.Empty<Team>(),
            new Dictionary<string, string>(StringComparer.Ordinal));
}
