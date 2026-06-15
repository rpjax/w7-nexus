using Nexus.Authorization.Application.Models;

namespace Nexus.Operator.Application.Contracts;

public interface IOperatorAccess
{
    Task<IAccessEvaluationResult<IOperator>> ResolveAsync(CancellationToken cancellationToken = default);
}
