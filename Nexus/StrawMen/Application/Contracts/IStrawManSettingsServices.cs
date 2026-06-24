using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.StrawMen.Aggregates;

namespace Nexus.StrawMen.Application.Contracts;

public interface IStrawManSettingsRepository : IRepository<StrawManSettings>
{
    new Task<StrawManSettings> CreateAsync(StrawManSettings entity);
}

public interface IStrawManSettingsQueryService
{
    Task<decimal> GetMovementFeePercentageAsync(string strawManId, CancellationToken cancellationToken = default);
    Task<IResult<StrawManSettingsDetails>> GetSettingsAsync(string strawManId, CancellationToken cancellationToken = default);
}

public interface IStrawManSettingsCommandService
{
    Task<IResult<StrawManSettingsDetails>> UpsertMovementFeePercentageAsync(
        string strawManId,
        decimal movementFeePercentage,
        string updatedByAdminId,
        CancellationToken cancellationToken = default);
}

public sealed class StrawManSettingsDetails
{
    public string StrawManId { get; init; } = string.Empty;
    public decimal MovementFeePercentage { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedByAdminId { get; init; }
}

public sealed class UpdateStrawManSettingsRequest
{
    public decimal MovementFeePercentage { get; init; }
}
