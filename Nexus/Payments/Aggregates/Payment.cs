using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Payments.ErrorCodes;

namespace Nexus.Payments.Aggregates;

public enum PaymentStatus
{
    Pending = 0,
    Paid,
    Refunded,
    Dead,
}

public enum PaymentGateway
{
    None = 0,
    FusionPay,
    Frendz,
    SuitPay,
    SigiloPay,
}

public sealed class Payment
{
    // Internal IDs (not related to the gateway)
    public string Id { get; }
    public string OperationId { get; }
    public string? OperatorAccountId { get; private set; }
    public string? StrawManAccountId { get; private set; }

    // Gateway References
    public PaymentGateway Gateway { get; private set; }
    public string GatewayTransactionId { get; private set; }

    // Payment Details
    public decimal Amount { get; }

    // Payment Status
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? DiedAt { get; private set; }
    public string? DeathReason { get; private set; }

    internal Payment(
        string id,
        string operationId,
        PaymentGateway gateway,
        string gatewayTransactionId,
        decimal amount,
        PaymentStatus status,
        string? operatorAccountId,
        string? strawManAccountId,
        DateTime createdAt,
        DateTime? paidAt,
        DateTime? refundedAt,
        DateTime? diedAt,
        string? deathReason)
    {
        Id = id;
        OperationId = operationId;
        Gateway = gateway;
        GatewayTransactionId = gatewayTransactionId;
        Amount = amount;

        Status = status;
        OperatorAccountId = operatorAccountId;
        StrawManAccountId = strawManAccountId;

        CreatedAt = createdAt;
        PaidAt = paidAt;
        RefundedAt = refundedAt;
        DiedAt = diedAt;
        DeathReason = deathReason;
    }

    public IResult BindToStrawMan(string strawManAccountId)
    {
        if (string.IsNullOrWhiteSpace(strawManAccountId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                .WithMessage("Straw man account ID cannot be empty")
                .Build());

        if (StrawManAccountId is not null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManAlreadyBound)
                .WithMessage("Payment is already bound to a straw man account")
                .Build());

        StrawManAccountId = strawManAccountId;
        return Result.Success();
    }

    public IResult BindToGateway(PaymentGateway gateway, string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPaymentIdInvalid)
                .WithMessage("Gateway transaction ID cannot be empty")
                .Build());

        if (gateway == PaymentGateway.None)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayInvalid)
                .WithMessage("Gateway is invalid")
                .Build());

        Gateway = gateway;
        GatewayTransactionId = transactionId.Trim();
        return Result.Success();
    }

    public IResult BindToOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        if (OperatorAccountId is not null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorAlreadyBound)
                .WithMessage("Payment is already bound to an operator")
                .Build());

        OperatorAccountId = operatorId;
        return Result.Success();
    }

    public IResult MarkAsPaid()
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage($"Cannot mark as paid from status {Status}")
                .Build());

        if (string.IsNullOrWhiteSpace(OperatorAccountId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorRequired)
                .WithMessage("Operator must be bound before marking payment as paid")
                .Build());

        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult Refund()
    {
        if (Status != PaymentStatus.Paid)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage($"Cannot refund payment with status {Status}")
                .Build());

        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult Die(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.DeathReasonRequired)
                .WithMessage("Death reason is required")
                .Build());

        if (Status == PaymentStatus.Dead)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AlreadyDead)
                .WithMessage("Payment is already dead")
                .Build());

        Status = PaymentStatus.Dead;
        DiedAt = DateTime.UtcNow;
        DeathReason = reason;
        return Result.Success();
    }

}
