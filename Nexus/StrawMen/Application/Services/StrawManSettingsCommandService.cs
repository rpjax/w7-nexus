using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.StrawMen.Aggregates;
using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Errors;

namespace Nexus.StrawMen.Application.Services;

public sealed class StrawManSettingsCommandService : IStrawManSettingsCommandService
{
    private readonly IAccountRepository _accounts;
    private readonly IStrawManSettingsRepository _settings;

    public StrawManSettingsCommandService(
        IAccountRepository accounts,
        IStrawManSettingsRepository settings)
    {
        _accounts = accounts;
        _settings = settings;
    }

    public async Task<IResult<StrawManSettingsDetails>> UpsertMovementFeePercentageAsync(
        string strawManId,
        decimal movementFeePercentage,
        string updatedByAdminId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;
        updatedByAdminId = updatedByAdminId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<StrawManSettingsDetails>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.StrawManIdInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var strawMan = _accounts.AsQueryable().FirstOrDefault(a => a.Id == strawManId);
        if (strawMan is null)
            return Result<StrawManSettingsDetails>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.StrawManNotFound)
                .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                .Build());

        if (!strawMan.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Result<StrawManSettingsDetails>.Failure(Error.Create()
                .WithCode(StrawManSettingsErrorCodes.StrawManRoleRequired)
                .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                .Build());

        var existing = _settings.AsQueryable()
            .FirstOrDefault(s => s.StrawManId == strawManId);

        if (existing is null)
        {
            var createResult = StrawManSettings.Create(strawManId, movementFeePercentage, updatedByAdminId);
            if (createResult.IsFailure)
                return Result<StrawManSettingsDetails>.Failure(createResult.Errors);

            var persisted = await _settings.CreateAsync(createResult.Value!);
            return Result<StrawManSettingsDetails>.Success(StrawManSettingsQueryService.ToDetails(persisted));
        }

        var updateResult = existing.UpdateMovementFeePercentage(movementFeePercentage, updatedByAdminId);
        if (updateResult.IsFailure)
            return Result<StrawManSettingsDetails>.Failure(updateResult.Errors);

        await _settings.UpdateAsync(existing);
        return Result<StrawManSettingsDetails>.Success(StrawManSettingsQueryService.ToDetails(existing));
    }
}
