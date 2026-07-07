using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<StrawManSettingsDetails>> GetStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage,
        CancellationToken cancellationToken = default);
}
