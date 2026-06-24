using Aidan.Core.Patterns;
using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Aggregates;

namespace Nexus.StrawMen.Application.Services;

public sealed class StrawManSettingsQueryService : IStrawManSettingsQueryService
{
    private readonly IStrawManSettingsRepository _settings;

    public StrawManSettingsQueryService(IStrawManSettingsRepository settings)
    {
        _settings = settings;
    }

    public Task<decimal> GetMovementFeePercentageAsync(
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(strawManId))
            return Task.FromResult(0m);

        var settings = _settings.AsQueryable()
            .FirstOrDefault(s => s.StrawManId == strawManId);

        return Task.FromResult(settings?.MovementFeePercentage ?? 0m);
    }

    public Task<IResult<StrawManSettingsDetails>> GetSettingsAsync(
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;

        var settings = _settings.AsQueryable()
            .FirstOrDefault(s => s.StrawManId == strawManId);

        if (settings is null)
        {
            return Task.FromResult<IResult<StrawManSettingsDetails>>(Result<StrawManSettingsDetails>.Success(
                new StrawManSettingsDetails
                {
                    StrawManId = strawManId,
                    MovementFeePercentage = 0m,
                }));
        }

        return Task.FromResult<IResult<StrawManSettingsDetails>>(Result<StrawManSettingsDetails>.Success(
            ToDetails(settings)));
    }

    internal static StrawManSettingsDetails ToDetails(StrawManSettings settings) =>
        new()
        {
            StrawManId = settings.StrawManId,
            MovementFeePercentage = settings.MovementFeePercentage,
            UpdatedAt = settings.UpdatedAt,
            UpdatedByAdminId = settings.UpdatedByAdminId,
        };
}
