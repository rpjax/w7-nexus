using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Payments.Errors;

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
    Wintech,
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
        string Id,
        string OperationId,
        PaymentGateway Gateway,
        string GatewayTransactionId,
        decimal Amount,
        PaymentStatus Status,
        string? OperatorAccountId,
        string? StrawManAccountId,
        DateTime CreatedAt,
        DateTime? PaidAt,
        DateTime? RefundedAt,
        DateTime? DiedAt,
        string? DeathReason)
    {
        this.Id = Id;
        this.OperationId = OperationId;
        this.Gateway = Gateway;
        this.GatewayTransactionId = GatewayTransactionId;
        this.Amount = Amount;

        this.Status = Status;
        this.OperatorAccountId = OperatorAccountId;
        this.StrawManAccountId = StrawManAccountId;

        this.CreatedAt = CreatedAt;
        this.PaidAt = PaidAt;
        this.RefundedAt = RefundedAt;
        this.DiedAt = DiedAt;
        this.DeathReason = DeathReason;
    }

    public IResult BindToStrawMan(string strawManAccountId)
    {
        if (string.IsNullOrWhiteSpace(strawManAccountId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                .WithMessage("O ID da conta laranja não pode estar vazio.")
                .Build());

        if (StrawManAccountId is not null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManAlreadyBound)
                .WithMessage("Este pagamento já está vinculado a uma conta laranja.")
                .Build());

        StrawManAccountId = strawManAccountId;
        return Result.Success();
    }

    public IResult BindToGateway(PaymentGateway gateway, string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPaymentIdInvalid)
                .WithMessage("O ID da transação no gateway não pode estar vazio.")
                .Build());

        if (gateway == PaymentGateway.None)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayInvalid)
                .WithMessage("O gateway informado é inválido.")
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
                .WithMessage("O ID do operador não pode estar vazio.")
                .Build());

        if (OperatorAccountId is not null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorAlreadyBound)
                .WithMessage("Este pagamento já está vinculado a um operador.")
                .Build());

        OperatorAccountId = operatorId;
        return Result.Success();
    }

    public IResult MarkAsPaid()
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage($"Não é possível marcar como pago a partir do status {DescribeStatus(Status)}.")
                .Build());

        if (string.IsNullOrWhiteSpace(OperatorAccountId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorRequired)
                .WithMessage("É necessário vincular um operador antes de marcar o pagamento como pago.")
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
                .WithMessage($"Não é possível reembolsar um pagamento com status {DescribeStatus(Status)}.")
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
                .WithMessage("O motivo do cancelamento é obrigatório.")
                .Build());

        if (Status == PaymentStatus.Dead)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AlreadyDead)
                .WithMessage("Este pagamento já foi cancelado.")
                .Build());

        Status = PaymentStatus.Dead;
        DiedAt = DateTime.UtcNow;
        DeathReason = reason;
        return Result.Success();
    }

    private static string DescribeStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "pendente",
        PaymentStatus.Paid => "pago",
        PaymentStatus.Refunded => "reembolsado",
        PaymentStatus.Dead => "cancelado",
        _ => status.ToString(),
    };

}
