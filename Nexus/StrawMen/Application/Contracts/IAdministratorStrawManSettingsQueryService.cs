using Aidan.Core.Patterns;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Contracts;

public interface IAdministratorStrawManSettingsQueryService
{
    Task<IResult<StrawManSettingsDetails>> GetStrawManSettingsAsync(string strawManId);
}
