using Nexus.Administrators.Application.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Administrators.Application.Mapping;

public static class OperationMapping
{
    public static OperationDetails ToOperationDetails(this Operation operation)
        => OperationDetailsMapper.Map(
            operation,
            Array.Empty<Team>(),
            new Dictionary<string, string>(StringComparer.Ordinal));
}
