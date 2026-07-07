using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests.Victim;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Errors;

namespace Nexus.Olx.Application.Services;

public sealed class Victim : IVictim
{
    private readonly IAdPatchQueryService _query;
    private readonly IChargeService _chargeService;

    public Victim(
        IAdPatchQueryService query,
        IChargeService chargeService)
    {
        _query = query;
        _chargeService = chargeService;
    }

    public Task<IResult<ListPatchedAdsResponse>> ListAdPatchesAsync(CancellationToken cancellationToken = default) =>
        _query.ListAllAsync(cancellationToken);

    public async Task<IResult<CreatePixPaymentResponse>> CreatePixPaymentAsync(
        CreatePixPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Result<CreatePixPaymentResponse>.Failure(Error.Create()
                .WithCode(AdPatchErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());
        }

        string? operatorId = null;
        var adId = request.AdId?.Trim();
        if (!string.IsNullOrWhiteSpace(adId))
        {
            var patch = await _query.FindByOperationAndAdAsync(operationId, adId, cancellationToken);

            if (patch is null)
            {
                return Result<CreatePixPaymentResponse>.Failure(Error.Create()
                    .WithCode(AdPatchErrorCodes.AdPatchNotFound)
                    .WithMessage($"Não foi encontrado patch para o anúncio '{adId}' na operação '{operationId}'.")
                    .Build());
            }

            if (string.IsNullOrWhiteSpace(patch.OperatorId))
            {
                return Result<CreatePixPaymentResponse>.Failure(Error.Create()
                    .WithCode(AdPatchErrorCodes.OperatorNotFound)
                    .WithMessage("Não há operador associado a este anúncio.")
                    .Build());
            }

            operatorId = patch.OperatorId;
        }

        var chargeResult = await _chargeService.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = operationId,
            OperatorId = operatorId,
            Amount = request.Value,
        });

        if (chargeResult.IsFailure)
            return Result<CreatePixPaymentResponse>.Failure(chargeResult.Errors);

        var charge = chargeResult.Value
            ?? throw new InvalidOperationException("Charge creation succeeded without a value.");

        return Result<CreatePixPaymentResponse>.Success(new CreatePixPaymentResponse
        {
            PixCode = charge.PixCode,
            Value = request.Value,
            ExpirationTimeSeconds = charge.ExpirationTimeSeconds,
            PaymentRecipient = charge.PaymentRecipient,
        });
    }
}
