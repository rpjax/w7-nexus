using Aidan.Core.Linq.Extensions;
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

    public async Task<decimal> GetMovementFeePercentageAsync(
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(strawManId))
            return 0m;

        var settings = await _settings.AsQueryable()
            .Where(s => s.StrawManId == strawManId)
            .FirstOrDefaultAsync();

        return settings?.MovementFeePercentage ?? 0m;
    }

    public async Task<IResult<StrawManSettingsDetails>> GetSettingsAsync(
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;

        var settings = await _settings.AsQueryable()
            .Where(s => s.StrawManId == strawManId)
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            return Result<StrawManSettingsDetails>.Success(
                new StrawManSettingsDetails
                {
                    StrawManId = strawManId,
                    MovementFeePercentage = 0m,
                });
        }

        return Result<StrawManSettingsDetails>.Success(ToDetails(settings));
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
