using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application;
using Nexus.Payments.Aggregates;
using Nexus.Payments.ErrorCodes;
using Nexus.Payments.Application;
using Nexus.Operations.Application;

namespace Nexus.Payments.Infrastructure;

public sealed class PixPaymentService : IPixPaymentService
{
    private IAccountRepository _accountRepository { get; }
    private IPixPaymentRepository _pixPaymentRepository { get; }
    private IOperationRepository _operationRepository { get; }

    public PixPaymentService(
        IAccountRepository accountRepository,
        IPixPaymentRepository pixPaymentRepository,
        IOperationRepository operationRepository)
    {
        _accountRepository = accountRepository;
        _pixPaymentRepository = pixPaymentRepository;
        _operationRepository = operationRepository;
    }

    public async Task<IResult<PixPayment>> CreatePixPaymentAsync(CreatePixPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize inputs to avoid accepting "   " values and to prevent nullability warnings later.
        var amount = request.Amount;
        var operationId = request.OperationId?.Trim();
        var gateway = request.Gateway;
        var gatewayPaymentId = request.GatewayPaymentId?.Trim();
        var operatorAccountId = request.OperatorAccountId?.Trim();
        var strawManAccountId = request.StrawManAccountId?.Trim();

        var builder = Result.Create<PixPayment>();

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

        var id = Guid.NewGuid().ToString("N");
        var payment = new PixPayment(id, operationId!, gateway, validatedGatewayPaymentId, amount);

        if (strawManAccountId is not null)
        {
            var bindStrawManResult = payment.BindToStrawMan(strawManAccountId);
            if (bindStrawManResult.IsFailure)
                return Result.Create<PixPayment>().WithErrors(bindStrawManResult.Errors).Build();
        }

        if (operatorAccountId is not null)
        {
            var bindOperatorResult = payment.BindToOperator(operatorAccountId);
            if (bindOperatorResult.IsFailure)
                return Result.Create<PixPayment>().WithErrors(bindOperatorResult.Errors).Build();
        }

        await _pixPaymentRepository.CreateAsync(payment);
        return builder.WithValue(payment).Build();
    }

    public async Task<IResult> PayAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _pixPaymentRepository.AsQueryable()
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

        await _pixPaymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> RefundAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _pixPaymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
                .Build());

        var result = payment.Refund();
        if (result.IsFailure)
            return result;

        await _pixPaymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> KillAsync(string paymentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("Payment ID is required")
                .Build());

        var payment = _pixPaymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"Payment '{paymentId}' was not found")
                .Build());

        var result = payment.Die(reason);
        if (result.IsFailure)
            return result;

        await _pixPaymentRepository.UpdateAsync(payment);
        return Result.Success();
    }
}