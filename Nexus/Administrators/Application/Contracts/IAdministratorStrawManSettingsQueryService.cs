using Aidan.Core.Patterns;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorStrawManSettingsQueryService
{
    Task<IResult<StrawManSettingsDetails>> GetStrawManSettingsAsync(string strawManId);
}
