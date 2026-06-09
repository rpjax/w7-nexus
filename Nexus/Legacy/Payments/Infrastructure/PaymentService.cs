using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Legacy.Operations.Application;
using Nexus.Legacy.Accounts.Application;
using Nexus.Legacy.Payments.Aggregates;
using Nexus.Legacy.Payments.Application;
using Nexus.Legacy.Payments.ErrorCodes;
using Nexus.Legacy.Payments.Application.Models;

namespace Nexus.Legacy.Payments.Infrastructure;

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

        // Normalize inputs to avoid accepting "   " values and to prevent nullability warnings later.
        var amount = request.Amount;
        var operationId = request.OperationId?.Trim();
        var gateway = request.Gateway;
        var gatewayPaymentId = request.GatewayPaymentId?.Trim();
        var operatorAccountId = request.OperatorAccountId?.Trim();
        var strawManAccountId = request.StrawManAccountId?.Trim();

        var builder = Result.Create<Payment>();

        if (string.IsNullOrWhiteSpace(operationId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required")
                .Build());

        if (amount <= 0m)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.AmountInvalid)
                .WithMessage("Amount must be greater than zero")
                .Build());

        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPaymentIdInvalid)
                .WithMessage("Gateway payment ID is required")
                .Build());

        if (gateway == PaymentGateway.None)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayInvalid)
                .WithMessage("Gateway is invalid")
                .Build());

        if (operatorAccountId is not null && string.IsNullOrWhiteSpace(operatorAccountId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty when provided")
                .Build());

        if (strawManAccountId is not null && string.IsNullOrWhiteSpace(strawManAccountId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                .WithMessage("Straw man account ID cannot be empty when provided")
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
                    .WithMessage($"Payment id '{explicitPaymentId}' is already in use")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var operationExists = _operationRepository.AsQueryable()
            .Any(o => o.Id == operationId);
        if (!operationExists)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationNotFound)
                .WithMessage($"Operation '{operationId}' was not found")
                .Build());

        if (operatorAccountId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == operatorAccountId);
            if (!operatorExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorAccountNotFound)
                    .WithMessage($"Operator account '{operatorAccountId}' was not found")
                    .Build());
        }

        if (strawManAccountId is not null)
        {
            var strawManExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == strawManAccountId);
            if (!strawManExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{strawManAccountId}' was not found")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        // At this point, gatewayPaymentId is validated (not null/empty/whitespace).
        var validatedGatewayPaymentId = gatewayPaymentId!;

        var id = string.IsNullOrWhiteSpace(explicitPaymentId)
            ? Guid.NewGuid().ToString("N")
            : explicitPaymentId!;
        var createdAt = DateTime.UtcNow;
        var payment = new Payment(
            id,
            operationId!,
            gateway,
            validatedGatewayPaymentId,
            amount,
            PaymentStatus.Pending,
            operatorAccountId: null,
            strawManAccountId: null,
            createdAt,
            paidAt: null,
            refundedAt: null,
            diedAt: null,
            deathReason: null);

        if (strawManAccountId is not null)
        {
            var bindStrawManResult = payment.BindToStrawMan(strawManAccountId);
            if (bindStrawManResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindStrawManResult.Errors).Build();
        }

        if (operatorAccountId is not null)
        {
            var bindOperatorResult = payment.BindToOperator(operatorAccountId);
            if (bindOperatorResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindOperatorResult.Errors).Build();
        }

        await _paymentRepository.CreateAsync(payment);
        return builder.WithValue(payment).Build();
    }

    public async Task<IResult> PayAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
                .Build());

        if (payment.OperatorAccountId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == payment.OperatorAccountId);
            if (!operatorExists)
                return Result.Failure(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorAccountNotFound)
                    .WithMessage($"Operator account '{payment.OperatorAccountId}' was not found")
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
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
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
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
                .Build());

        var result = payment.Die(reason);
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
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
                .Build());

        await _paymentRepository.DeleteAsync(payment);
        return Result.Success();
    }
}