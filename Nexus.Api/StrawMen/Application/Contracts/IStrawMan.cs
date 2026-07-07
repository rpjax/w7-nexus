using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;

namespace Nexus.StrawMen.Application.Contracts;

public interface IStrawMan
{
    Task<IOperationResult<StrawManSettingsDetails>> GetSettingsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default);
}
