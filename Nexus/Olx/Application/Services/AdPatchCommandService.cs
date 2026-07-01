using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Errors;
using Nexus.Operations.Application.Contracts;

namespace Nexus.Olx.Application.Services;

public sealed class AdPatchCommandService : IAdPatchCommandService
{
    private readonly IAdPatchRepository _adPatches;
    private readonly IOperationRepository _operations;
    private readonly ITeamRepository _teams;
    private readonly IAccountRepository _accounts;

    public AdPatchCommandService(
        IAdPatchRepository adPatches,
        IOperationRepository operations,
        ITeamRepository teams,
        IAccountRepository accounts)
    {
        _adPatches = adPatches;
        _operations = operations;
        _teams = teams;
        _accounts = accounts;
    }

    public async Task<IResult<ImpersonateAdResponse>> ImpersonateAdAsync(
        string requesterAccountId,
        ImpersonateAdRequest request,
        bool requireSelfOperator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        var adId = request.AdId?.Trim() ?? string.Empty;
        var adUrl = request.AdUrl?.Trim() ?? string.Empty;
        var operatorId = request.OperatorId?.Trim() ?? string.Empty;
        requesterAccountId = requesterAccountId?.Trim() ?? string.Empty;

        var validation = await ValidateOperationAndAdAsync(operationId, adId, adUrl, cancellationToken);
        if (validation.IsFailure)
            return Result<ImpersonateAdResponse>.Failure(validation.Errors);

        var operatorValidation = await ValidateOperatorAsync(
            requesterAccountId,
            operatorId,
            operationId,
            requireSelfOperator,
            cancellationToken);
        if (operatorValidation.IsFailure)
            return Result<ImpersonateAdResponse>.Failure(operatorValidation.Errors);

        var existing = await _adPatches.AsQueryable()
            .Where(s => s.OperationId == operationId && s.AdId == adId)
            .FirstOrDefaultAsync();

        AdPatch patch;
        if (existing is null)
        {
            var createResult = AdPatch.Create(operationId, adId, adUrl);
            if (createResult.IsFailure)
                return Result<ImpersonateAdResponse>.Failure(createResult.Errors);

            patch = createResult.Value!;
            var impersonateResult = patch.Impersonate(operatorId);
            if (impersonateResult.IsFailure)
                return Result<ImpersonateAdResponse>.Failure(impersonateResult.Errors);

            await _adPatches.CreateAsync(patch);
        }
        else
        {
            patch = existing;
            var adUrlResult = patch.EnsureAdUrl(adUrl);
            if (adUrlResult.IsFailure)
                return Result<ImpersonateAdResponse>.Failure(adUrlResult.Errors);

            var impersonateResult = patch.Impersonate(operatorId);
            if (impersonateResult.IsFailure)
                return Result<ImpersonateAdResponse>.Failure(impersonateResult.Errors);

            await _adPatches.UpdateAsync(patch);
        }

        return Result<ImpersonateAdResponse>.Success(new ImpersonateAdResponse());
    }

    public async Task<IResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        string requesterAccountId,
        UnimpersonateAdRequest request,
        bool requireSelfOperator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        var adId = request.AdId?.Trim() ?? string.Empty;
        var operatorId = request.OperatorId?.Trim() ?? string.Empty;
        requesterAccountId = requesterAccountId?.Trim() ?? string.Empty;

        var validation = await ValidateOperationAndAdAsync(operationId, adId, adUrl: null, cancellationToken);
        if (validation.IsFailure)
            return Result<UnimpersonateAdResponse>.Failure(validation.Errors);

        var operatorValidation = await ValidateOperatorAsync(
            requesterAccountId,
            operatorId,
            operationId,
            requireSelfOperator,
            cancellationToken);
        if (operatorValidation.IsFailure)
            return Result<UnimpersonateAdResponse>.Failure(operatorValidation.Errors);

        var patch = await _adPatches.AsQueryable()
            .Where(s => s.OperationId == operationId && s.AdId == adId)
            .FirstOrDefaultAsync();

        if (patch is null)
            return Result<UnimpersonateAdResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.AdPatchNotFound)
                .WithMessage($"O patch do anúncio '{adId}' não foi encontrado.")
                .Build());

        if (!patch.IsImpersonating)
            return Result<UnimpersonateAdResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.NotImpersonating)
                .WithMessage("O anúncio não está sendo impersonado.")
                .Build());

        if (!string.Equals(patch.OperatorId, operatorId, StringComparison.Ordinal))
            return Result<UnimpersonateAdResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.ImpersonationOperatorMismatch)
                .WithMessage("O operador informado não corresponde à impersonação ativa.")
                .Build());

        var unimpersonateResult = patch.Unimpersonate();
        if (unimpersonateResult.IsFailure)
            return Result<UnimpersonateAdResponse>.Failure(unimpersonateResult.Errors);

        await _adPatches.UpdateAsync(patch);
        return Result<UnimpersonateAdResponse>.Success(new UnimpersonateAdResponse());
    }

    public async Task<IResult<UpdateAdDetailsPatchResponse>> UpdateAdDetailsPatchAsync(
        string requesterAccountId,
        UpdateAdDetailsPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        var adId = request.AdId?.Trim() ?? string.Empty;
        requesterAccountId = requesterAccountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requesterAccountId))
            return Result<UpdateAdDetailsPatchResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorIdInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());

        var validation = await ValidateOperationAndAdAsync(operationId, adId, adUrl: null, cancellationToken);
        if (validation.IsFailure)
            return Result<UpdateAdDetailsPatchResponse>.Failure(validation.Errors);

        var operatorValidation = await ValidateOperatorAsync(
            requesterAccountId,
            requesterAccountId,
            operationId,
            requireSelfOperator: true,
            cancellationToken);
        if (operatorValidation.IsFailure)
            return Result<UpdateAdDetailsPatchResponse>.Failure(operatorValidation.Errors);

        var existing = await _adPatches.AsQueryable()
            .Where(s => s.OperationId == operationId && s.AdId == adId)
            .FirstOrDefaultAsync();

        if (existing is null)
            return Result<UpdateAdDetailsPatchResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.AdPatchNotFound)
                .WithMessage($"O patch do anúncio '{adId}' não foi encontrado.")
                .Build());

        var patch = existing;

        var updateResult = patch.UpdatePricePatch(requesterAccountId, request.OriginalPrice, request.PromotionalPrice);
        if (updateResult.IsFailure)
            return Result<UpdateAdDetailsPatchResponse>.Failure(updateResult.Errors);

        await _adPatches.UpdateAsync(patch);

        return Result<UpdateAdDetailsPatchResponse>.Success(new UpdateAdDetailsPatchResponse());
    }

    private async Task<IResult> ValidateOperationAndAdAsync(
        string operationId,
        string adId,
        string? adUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(adId))
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.AdIdInvalid)
                .WithMessage("O ID do anúncio é obrigatório.")
                .Build());

        if (adUrl is not null)
        {
            var urlValidation = AdPatch.TryNormalizeAdUrl(adUrl, out _);
            if (urlValidation.IsFailure)
                return urlValidation;
        }

        var operation = await _operations.AsQueryable()
            .Where(o => o.Id == operationId)
            .FirstOrDefaultAsync();

        if (operation is null)
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());

        return Result.Success();
    }

    private async Task<IResult> ValidateOperatorAsync(
        string requesterAccountId,
        string operatorId,
        string operationId,
        bool requireSelfOperator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorIdInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());

        if (requireSelfOperator && !string.Equals(operatorId, requesterAccountId, StringComparison.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorScopeMismatch)
                .WithMessage("Você só pode operar sobre o seu próprio ID de operador.")
                .Build());

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == operatorId)
            .FirstOrDefaultAsync();

        if (account is null)
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorNotFound)
                .WithMessage($"A conta do operador '{operatorId}' não foi encontrada.")
                .Build());

        if (!account.Roles.Contains(Roles.Operator, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorRoleRequired)
                .WithMessage($"A conta '{operatorId}' não possui o perfil de operador.")
                .Build());

        var isAssigned = await _teams.AsQueryable()
            .Where(t => t.OperationId == operationId && t.OperatorIds.Contains(operatorId))
            .AnyAsync();

        if (!isAssigned)
            return Result.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperatorNotAssignedToOperation)
                .WithMessage($"O operador '{operatorId}' não está atribuído à operação '{operationId}'.")
                .Build());

        return Result.Success();
    }
}
