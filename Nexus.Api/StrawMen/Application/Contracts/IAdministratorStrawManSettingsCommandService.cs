using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Contracts;

public interface IAdministratorStrawManSettingsCommandService
{
    Task<IResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage);
}
