using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Services;

public sealed class AdministratorStrawManSettingsCommandService : IAdministratorStrawManSettingsCommandService
{
    private readonly IStrawManSettingsCommandService _settings;

    public AdministratorStrawManSettingsCommandService(IStrawManSettingsCommandService settings)
    {
        _settings = settings;
    }

    public Task<IResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage) =>
        _settings.UpsertMovementFeePercentageAsync(
            strawManId,
            movementFeePercentage,
            identity.AccountId);
}
