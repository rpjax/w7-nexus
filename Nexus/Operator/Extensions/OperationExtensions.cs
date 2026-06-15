using Nexus.Operator.Application.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operator.Extensions;

public static class OperationExtensions
{
    public static OperationDetails ToOperationDetails(this Operation operation)
        => OperationDetailsMapper.Map(
            operation,
            Array.Empty<Team>(),
            new Dictionary<string, string>(StringComparer.Ordinal));
}
