using Nexus.Authorization.Application.Models;

namespace Nexus.StrawMan.Application.Contracts;

public interface IStrawManAccess
{
    Task<IAccessEvaluationResult<IStrawMan>> ResolveAsync(CancellationToken cancellationToken = default);
}
