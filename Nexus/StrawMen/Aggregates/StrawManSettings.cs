using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.StrawMen.Errors;

namespace Nexus.StrawMen.Aggregates;

public sealed class StrawManSettings
{
    public const decimal MaxMovementFeePercentage = 100m;

    public string StrawManId { get; }
    public decimal MovementFeePercentage { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedByAdminId { get; private set; }

    internal StrawManSettings(
        string strawManId,
        decimal movementFeePercentage,
        DateTime updatedAt,
        string updatedByAdminId)
    {
        StrawManId = strawManId;
        MovementFeePercentage = movementFeePercentage;
        UpdatedAt = updatedAt;
        UpdatedByAdminId = updatedByAdminId;
    }

    public static IResult<StrawManSettings> Create(
        string strawManId,
        decimal movementFeePercentage,
        string updatedByAdminId)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;
        updatedByAdminId = updatedByAdminId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<StrawManSettings>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.StrawManIdInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(updatedByAdminId))
            return Result<StrawManSettings>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.AdminIdInvalid)
                .WithMessage("O ID do administrador é obrigatório.")
                .Build());

        if (movementFeePercentage < 0 || movementFeePercentage > MaxMovementFeePercentage)
            return Result<StrawManSettings>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.MovementFeePercentageInvalid)
                .WithMessage($"A taxa de movimentação deve estar entre 0 e {MaxMovementFeePercentage}.")
                .Build());

        return Result<StrawManSettings>.Success(new StrawManSettings(
            strawManId,
            movementFeePercentage,
            DateTime.UtcNow,
            updatedByAdminId));
    }

    public IResult UpdateMovementFeePercentage(decimal movementFeePercentage, string updatedByAdminId)
    {
        updatedByAdminId = updatedByAdminId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(updatedByAdminId))
            return Result.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.AdminIdInvalid)
                .WithMessage("O ID do administrador é obrigatório.")
                .Build());

        if (movementFeePercentage < 0 || movementFeePercentage > MaxMovementFeePercentage)
            return Result.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.MovementFeePercentageInvalid)
                .WithMessage($"A taxa de movimentação deve estar entre 0 e {MaxMovementFeePercentage}.")
                .Build());

        MovementFeePercentage = movementFeePercentage;
        UpdatedByAdminId = updatedByAdminId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
