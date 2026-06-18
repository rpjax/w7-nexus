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

public enum PaymentSettlementStatus
{
    Unsettled = 0,
    Withdrawn,
}

public sealed class PaymentSplit
{
    public string AccountId { get; }
    public decimal Percentage { get; }
    public decimal Amount { get; }

    internal PaymentSplit(string accountId, decimal percentage, decimal amount)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
        Amount = amount;
    }

    public static IReadOnlyList<PaymentSplit> CreateSnapshot(
        decimal paymentAmount,
        IReadOnlyList<(string AccountId, decimal Percentage)> cuts)
    {
        if (cuts.Count == 0)
            return Array.Empty<PaymentSplit>();

        var splits = new List<PaymentSplit>(cuts.Count);
        var allocated = 0m;

        for (var i = 0; i < cuts.Count; i++)
        {
            var (accountId, percentage) = cuts[i];
            decimal amount;

            if (i == cuts.Count - 1)
                amount = Round(paymentAmount - allocated);
            else
            {
                amount = Round(paymentAmount * percentage / 100m);
                allocated += amount;
            }

            splits.Add(new PaymentSplit(accountId, percentage, amount));
        }

        return splits;
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class Payment
{
    // Internal IDs (not related to the gateway)
    public string Id { get; }
    public string OperationId { get; }
    public string TeamId { get; }
    public string? OperatorAccountId { get; private set; }
    public string? StrawManAccountId { get; private set; }

    // Gateway References
    public PaymentGateway Gateway { get; private set; }
    public string GatewayTransactionId { get; private set; }

    // Payment Details
    public decimal Amount { get; }

    // Split Details
    public IReadOnlyList<PaymentSplit> Splits { get; }

    // Payment Status
    public PaymentStatus Status { get; private set; }
    public PaymentSettlementStatus SettlementStatus { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? DiedAt { get; private set; }
    public string? DeathReason { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }

    internal Payment(
        string Id,
        string OperationId,
        string TeamId,
        PaymentGateway Gateway,
        string GatewayTransactionId,
        decimal Amount,
        IReadOnlyList<PaymentSplit> Splits,
        PaymentStatus Status,
        PaymentSettlementStatus SettlementStatus,
        string? OperatorAccountId,
        string? StrawManAccountId,
        DateTime CreatedAt,
        DateTime? PaidAt,
        DateTime? RefundedAt,
        DateTime? DiedAt,
        string? DeathReason,
        DateTime? WithdrawnAt)
    {
        this.Id = Id;
        this.OperationId = OperationId;
        this.TeamId = TeamId;
        this.Gateway = Gateway;
        this.GatewayTransactionId = GatewayTransactionId;
        this.Amount = Amount;
        this.Splits = Splits;

        this.Status = Status;
        this.SettlementStatus = SettlementStatus;
        this.OperatorAccountId = OperatorAccountId;
        this.StrawManAccountId = StrawManAccountId;

        this.CreatedAt = CreatedAt;
        this.PaidAt = PaidAt;
        this.RefundedAt = RefundedAt;
        this.DiedAt = DiedAt;
        this.DeathReason = DeathReason;
        this.WithdrawnAt = WithdrawnAt;
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

        if (Splits.Count == 0)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.SplitsRequired)
                .WithMessage("É necessário definir o split de repasse antes de marcar o pagamento como pago.")
                .Build());

        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult MarkAsWithdrawn()
    {
        if (SettlementStatus == PaymentSettlementStatus.Withdrawn)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AlreadyWithdrawn)
                .WithMessage("Este pagamento já foi sacado do gateway.")
                .Build());

        if (Status != PaymentStatus.Paid)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidSettlementTransition)
                .WithMessage($"Não é possível sacar um pagamento com status {DescribeStatus(Status)}.")
                .Build());

        if (SettlementStatus != PaymentSettlementStatus.Unsettled)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidSettlementTransition)
                .WithMessage($"Não é possível sacar um pagamento com liquidação {DescribeSettlementStatus(SettlementStatus)}.")
                .Build());

        SettlementStatus = PaymentSettlementStatus.Withdrawn;
        WithdrawnAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult Refund()
    {
        if (Status != PaymentStatus.Paid)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage($"Não é possível reembolsar um pagamento com status {DescribeStatus(Status)}.")
                .Build());

        if (SettlementStatus == PaymentSettlementStatus.Withdrawn)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage("Não é possível reembolsar um pagamento que já foi sacado do gateway.")
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

    private static string DescribeSettlementStatus(PaymentSettlementStatus status) => status switch
    {
        PaymentSettlementStatus.Unsettled => "pendente de saque",
        PaymentSettlementStatus.Withdrawn => "sacado",
        _ => status.ToString(),
    };

}
