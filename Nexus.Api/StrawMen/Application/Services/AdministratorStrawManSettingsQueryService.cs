using Aidan.Core.Patterns;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Services;

public sealed class AdministratorStrawManSettingsQueryService : IAdministratorStrawManSettingsQueryService
{
    private readonly IStrawManSettingsQueryService _settings;

    public AdministratorStrawManSettingsQueryService(IStrawManSettingsQueryService settings)
    {
        _settings = settings;
    }

    public Task<IResult<StrawManSettingsDetails>> GetStrawManSettingsAsync(string strawManId) =>
        _settings.GetSettingsAsync(strawManId);
}
