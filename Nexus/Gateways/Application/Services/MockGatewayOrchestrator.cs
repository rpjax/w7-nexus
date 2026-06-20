using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Gateways.Application.Services;

/// <summary>
/// Development-only orchestrator that skips external gateway APIs and always returns a mock PIX charge.
/// </summary>
public sealed class MockGatewayOrchestrator : IGatewayOrchestrator
{
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }
    private IPaymentService _paymentService { get; }
    private IPaymentRepository _paymentRepository { get; }
    private ILogger<MockGatewayOrchestrator> _logger { get; }

    public MockGatewayOrchestrator(
        IOperationRepository operationRepository,
        ITeamRepository teamRepository,
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        ILogger<MockGatewayOrchestrator> logger)
    {
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<IResult<GatewayPix>> CreateGatewayPixAsync(CreateGatewayPixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim();
        if (string.IsNullOrWhiteSpace(operationId))
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                    .WithMessage("O ID da operação é obrigatório.")
                    .Build())
                .Build();

        if (request.Amount <= 0m)
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.AmountInvalid)
                    .WithMessage("O valor deve ser maior que zero.")
                    .Build())
                .Build();

        var operationExists = _operationRepository.AsQueryable()
            .Any(o => o.Id == operationId);

        if (!operationExists)
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationNotFound)
                    .WithMessage($"A operação '{operationId}' não foi encontrada.")
                    .Build())
                .Build();

        var operatorAccountId = request.OperatorAccountId?.Trim();
        if (operatorAccountId is not null && string.IsNullOrWhiteSpace(operatorAccountId))
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                    .WithMessage("O ID do operador não pode estar vazio quando informado.")
                    .Build())
                .Build();

        var strawManAccountId = request.StrawManAccountId?.Trim();
        if (strawManAccountId is not null && string.IsNullOrWhiteSpace(strawManAccountId))
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                    .WithMessage("O ID da conta laranja não pode estar vazio quando informado.")
                    .Build())
                .Build();

        var team = string.IsNullOrWhiteSpace(operatorAccountId)
            ? null
            : _teamRepository.AsQueryable()
                .FirstOrDefault(t =>
                    t.OperationId == operationId &&
                    t.OperatorIds.Contains(operatorAccountId));

        var paymentId = string.IsNullOrWhiteSpace(request.PaymentId)
            ? ObjectId.GenerateNewId().ToString()
            : request.PaymentId.Trim();

        var gatewayTransactionId = $"mock-{paymentId}";

        var createPaymentRequest = new CreatePaymentRequest
        {
            ExplicitPaymentId = paymentId,
            OperationId = operationId,
            TeamId = team?.Id,
            OperatorAccountId = operatorAccountId,
            StrawManAccountId = strawManAccountId,
            Gateway = PaymentGateway.Frendz,
            Amount = request.Amount,
            GatewayPaymentId = gatewayTransactionId,
        };

        var createPaymentResult = await _paymentService.CreatePaymentAsync(createPaymentRequest);
        if (createPaymentResult.IsFailure)
        {
            return new ResultBuilder<GatewayPix>()
                .WithErrors(createPaymentResult.Errors)
                .Build();
        }

        var payment = createPaymentResult.Value
            ?? throw new InvalidOperationException("Payment creation succeeded without a value.");

        var bindGateway = payment.BindToGateway(PaymentGateway.Frendz, gatewayTransactionId);
        if (bindGateway.IsFailure)
        {
            await _paymentService.DeletePaymentAsync(payment.Id);
            return Result.Create<GatewayPix>()
                .WithErrors(bindGateway.Errors)
                .Build();
        }

        await _paymentRepository.UpdateAsync(payment);

        var gatewayPix = new GatewayPix
        {
            Id = gatewayTransactionId,
            Code = BuildMockPixCode(payment.Id, request.Amount),
        };
        
        _logger.LogWarning(
            "MockGatewayOrchestrator generated PIX for payment {PaymentId} (operation {OperationId}, amount {Amount}). No external gateway was called.",
            payment.Id,
            operationId,
            request.Amount);

        return Result.Create<GatewayPix>()
            .WithValue(gatewayPix)
            .Build();
    }

    private static string BuildMockPixCode(string paymentId, decimal amount)
    {
        var amountText = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        return $"00020126580014BR.GOV.BCB.PIX0136MOCK-{paymentId}52040000530398654{amountText.Length:D2}{amountText}5802BR5925NEXUS MOCK GATEWAY6009SAO PAULO62070503***6304MOCK";
    }
}
