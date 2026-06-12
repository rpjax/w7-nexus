using Nexus.Actors.Contracts;
using Nexus.Authorization.Application.Models;

namespace Nexus.Authorization.Application.Services.Contracts;

public interface IOperatorAccess
{
    Task<IAccessEvaluationResult<IOperator>> ResolveAsync(CancellationToken cancellationToken = default);
}
