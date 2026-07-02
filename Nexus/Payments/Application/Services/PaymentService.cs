using Aidan.Core.Errors;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private IAccountRepository _accountRepository { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IOperationRepository _operationRepository { get; }

    public PaymentService(
        IAccountRepository accountRepository,
        IPaymentRepository pixPaymentRepository,
        IOperationRepository operationRepository)
    {
        _accountRepository = accountRepository;
        _paymentRepository = pixPaymentRepository;
        _operationRepository = operationRepository;
    }

    public async Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var amount = request.Amount;
        var operationId = request.OperationId?.Trim();
        var gateway = request.Gateway;
        var gatewayPaymentId = request.GatewayPaymentId?.Trim();
        var operatorId = request.OperatorId?.Trim();
        var strawManId = request.StrawManId?.Trim();
        var splits = request.Splits ?? Array.Empty<PaymentSplit>();

        var builder = Result.Create<Payment>();

        if (string.IsNullOrWhiteSpace(operationId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        if (amount <= 0m)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.AmountInvalid)
                .WithMessage("O valor deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPaymentIdInvalid)
                .WithMessage("O ID do pagamento no gateway é obrigatório.")
                .Build());

        if (gateway == PaymentGateway.None)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayInvalid)
                .WithMessage("O gateway informado é inválido.")
                .Build());

        if (operatorId is not null && string.IsNullOrWhiteSpace(operatorId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                .WithMessage("O ID do operador não pode estar vazio quando informado.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var explicitPaymentId = request.ExplicitPaymentId?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitPaymentId))
        {
            var idTaken = await _paymentRepository.AsQueryable()
                .AnyAsync(p => p.Id == explicitPaymentId);
            if (idTaken)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.PaymentIdAlreadyExists)
                    .WithMessage($"O ID de pagamento '{explicitPaymentId}' já está em uso.")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var operation = _operationRepository.AsQueryable()
            .FirstOrDefault(o => o.Id == operationId);

        if (operation is null)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());

        if (operatorId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == operatorId);
            if (!operatorExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorNotFound)
                    .WithMessage($"A conta do operador '{operatorId}' não foi encontrada.")
                    .Build());
        }

        if (!string.IsNullOrWhiteSpace(strawManId))
        {
            var strawManAccount = _accountRepository.AsQueryable()
                .FirstOrDefault(a => a.Id == strawManId);
            if (strawManAccount is null)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManNotFound)
                    .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                    .Build());
            else if (!strawManAccount.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManRoleRequired)
                    .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var validatedGatewayPaymentId = gatewayPaymentId!;
        var id = string.IsNullOrWhiteSpace(explicitPaymentId) ? string.Empty : explicitPaymentId!;
        var createdAt = DateTime.UtcNow;
        var payment = new Payment(
            id,
            operationId!,
            gateway,
            validatedGatewayPaymentId,
            amount,
            splits,
            PaymentStatus.Pending,
            PaymentSettlementStatus.Unsettled,
            PaymentDistributionStatus.Pending,
            OperatorId: null,
            strawManId: strawManId ?? string.Empty,
            createdAt,
            PaidAt: null,
            RefundedAt: null,
            KilledAt: null,
            KillReason: null,
            WithdrawnAt: null,
            DistributedAt: null);

        if (operatorId is not null)
        {
            var bindOperatorResult = payment.BindToOperator(operatorId);
            if (bindOperatorResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindOperatorResult.Errors).Build();
        }

        payment = await _paymentRepository.CreateAsync(payment);
        return builder.WithValue(payment).Build();
    }

    public async Task<IResult<Payment>> GetByIdAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result<Payment>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result<Payment>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        return Result<Payment>.Success(payment);
    }

    public async Task<IResult<Payment>> BindOperatorAsync(string paymentId, string operatorId)
    {
        var paymentResult = await GetByIdAsync(paymentId);
        if (paymentResult.IsFailure)
            return paymentResult;

        var payment = paymentResult.Value!;
        var bindResult = payment.BindToOperator(operatorId);
        if (bindResult.IsFailure)
            return Result<Payment>.Failure(bindResult.Errors);

        await _paymentRepository.UpdateAsync(payment);
        return Result<Payment>.Success(payment);
    }

    public async Task<IResult<Payment>> BindStrawManAsync(string paymentId, BindPaymentStrawManRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentResult = await GetByIdAsync(paymentId);
        if (paymentResult.IsFailure)
            return paymentResult;

        var payment = paymentResult.Value!;
        var bindResult = payment.BindToStrawMan(request.StrawManId);
        if (bindResult.IsFailure)
            return Result<Payment>.Failure(bindResult.Errors);

        if (request.Splits is { Count: > 0 })
        {
            var replaceResult = payment.ReplaceSplits(request.Splits);
            if (replaceResult.IsFailure)
                return Result<Payment>.Failure(replaceResult.Errors);
        }

        await _paymentRepository.UpdateAsync(payment);
        return Result<Payment>.Success(payment);
    }

    public async Task<IResult> PayAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        if (payment.OperatorId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == payment.OperatorId);
            if (!operatorExists)
                return Result.Failure(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorNotFound)
                    .WithMessage($"A conta do operador '{payment.OperatorId}' não foi encontrada.")
                    .Build());
        }

        var result = payment.MarkAsPaid();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> RefundAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.Refund();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> KillAsync(string paymentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.Kill(reason);
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> MarkAsDistributedAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.MarkAsDistributed();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> DeletePaymentAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        await _paymentRepository.DeleteAsync(payment);
        return Result.Success();
    }
}
