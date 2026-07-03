using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using Nexus.Charges.Application;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Charges.Application.Services;

public sealed class ChargeService : IChargeService
{
    private IGatewayCredentialsResolver _credentialsResolver { get; }
    private IPaymentService _paymentService { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IChargeProfitShareResolver _profitShareResolver { get; }
    private IChargeSplitCalculationService _splitCalculation { get; }
    private IGatewayOrchestrator _gatewayOrchestrator { get; }

    public ChargeService(
        IGatewayCredentialsResolver credentialsResolver,
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IChargeProfitShareResolver profitShareResolver,
        IChargeSplitCalculationService splitCalculation,
        IGatewayOrchestrator gatewayOrchestrator)
    {
        _credentialsResolver = credentialsResolver;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _profitShareResolver = profitShareResolver;
        _splitCalculation = splitCalculation;
        _gatewayOrchestrator = gatewayOrchestrator;
    }

    public async Task<IResult<CreatePixChargeResponse>> CreatePixChargeAsync(CreatePixChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m)
        {
            return Result.Create<CreatePixChargeResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.AmountInvalid)
                    .WithMessage("O valor deve ser maior que zero.")
                    .Build())
                .Build();
        }

        var credentialsResult = await _credentialsResolver.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = request.OperationId,
            OperatorId = request.OperatorId,
        });

        if (credentialsResult.IsFailure)
        {
            return new ResultBuilder<CreatePixChargeResponse>()
                .WithErrors(credentialsResult.Errors)
                .Build();
        }

        var resolved = credentialsResult.Value
            ?? throw new InvalidOperationException("Credential resolution succeeded without a value.");

        var operationId = request.OperationId?.Trim() ?? string.Empty;
        var operatorId = request.OperatorId?.Trim();
        var paymentId = ObjectId.GenerateNewId().ToString();

        var splitsResult = await _profitShareResolver.ResolveSplitsAsync(
            operationId,
            operatorId,
            request.Amount);

        if (splitsResult.IsFailure)
        {
            return new ResultBuilder<CreatePixChargeResponse>()
                .WithErrors(splitsResult.Errors)
                .Build();
        }

        var createPaymentResult = await _paymentService.CreatePaymentAsync(new CreatePaymentRequest
        {
            ExplicitPaymentId = paymentId,
            OperationId = operationId,
            OperatorId = operatorId,
            StrawManId = null,
            Gateway = PaymentGateway.None,
            Amount = request.Amount,
            GatewayPaymentId = null,
            Splits = splitsResult.Value,
        });

        if (createPaymentResult.IsFailure)
        {
            return new ResultBuilder<CreatePixChargeResponse>()
                .WithErrors(createPaymentResult.Errors)
                .Build();
        }

        var payment = createPaymentResult.Value
            ?? throw new InvalidOperationException("Payment creation succeeded without a value.");

        var gatewayResult = await _gatewayOrchestrator.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = payment.Id,
            Amount = request.Amount,
            Credentials = resolved.Credentials,
        });

        if (gatewayResult.IsFailure)
        {
            await _paymentService.DeletePaymentAsync(payment.Id);
            return new ResultBuilder<CreatePixChargeResponse>()
                .WithErrors(gatewayResult.Errors)
                .Build();
        }

        var tryPix = gatewayResult.Value
            ?? throw new InvalidOperationException("Gateway PIX creation succeeded without a value.");

        if (!resolved.StrawManIdByCredentialId.TryGetValue(tryPix.CredentialId, out var strawManId))
        {
            await _paymentService.DeletePaymentAsync(payment.Id);
            return Result.Create<CreatePixChargeResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.GatewayPixFailed)
                    .WithMessage("A credencial retornada pelo gateway não foi encontrada.")
                    .Build())
                .Build();
        }

        var bindGateway = payment.BindToGateway(tryPix.Gateway, tryPix.TransactionId);
        if (bindGateway.IsFailure)
        {
            await _paymentService.DeletePaymentAsync(payment.Id);
            return Result.Create<CreatePixChargeResponse>()
                .WithErrors(bindGateway.Errors)
                .Build();
        }

        var bindStrawMan = payment.BindToStrawMan(strawManId);
        if (bindStrawMan.IsFailure)
        {
            await _paymentService.DeletePaymentAsync(payment.Id);
            return Result.Create<CreatePixChargeResponse>()
                .WithErrors(bindStrawMan.Errors)
                .Build();
        }

        if (payment.Splits.Count > 0)
        {
            var profitShareSplits = payment.Splits
                .Where(s => s.SplitKind == PaymentSplitKind.ProfitShare)
                .ToList();

            var recalculated = await _splitCalculation.ApplyStrawManFeeAsync(
                payment.Amount,
                profitShareSplits,
                strawManId);

            var replaceSplits = payment.ReplaceSplits(recalculated);
            if (replaceSplits.IsFailure)
            {
                await _paymentService.DeletePaymentAsync(payment.Id);
                return Result.Create<CreatePixChargeResponse>()
                    .WithErrors(replaceSplits.Errors)
                    .Build();
            }
        }

        await _paymentRepository.UpdateAsync(payment);

        return Result.Create<CreatePixChargeResponse>()
            .WithValue(new CreatePixChargeResponse
            {
                Id = tryPix.TransactionId,
                PixCode = tryPix.PixCode,
                PaymentRecipient = ChargeDefaults.PaymentRecipient,
                ExpirationTimeSeconds = ChargeDefaults.ExpirationTimeSeconds,
            })
            .Build();
    }
}
