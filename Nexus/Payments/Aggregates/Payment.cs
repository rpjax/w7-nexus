using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Aggregates;

public enum PaymentStatus
{
    Pending = 0,
    Paid,
    Refunded,
    Killed,
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

public enum PaymentDistributionStatus
{
    Pending = 0,
    Complete,
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

    public static IReadOnlyList<PaymentSplit> AllocateFromCuts(
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
    public string Id { get; }
    public string OperationId { get; }
    public string? OperatorId { get; private set; }
    public string StrawManId { get; private set; }

    public PaymentGateway Gateway { get; private set; }
    public string GatewayTransactionId { get; private set; }

    public decimal Amount { get; }

    public IReadOnlyList<PaymentSplit> Splits { get; }

    public PaymentStatus Status { get; private set; }
    public PaymentSettlementStatus SettlementStatus { get; private set; }
    public PaymentDistributionStatus DistributionStatus { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? KilledAt { get; private set; }
    public string? KillReason { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public DateTime? DistributedAt { get; private set; }

    internal Payment(
        string Id,
        string OperationId,
        PaymentGateway Gateway,
        string GatewayTransactionId,
        decimal Amount,
        IReadOnlyList<PaymentSplit> Splits,
        PaymentStatus Status,
        PaymentSettlementStatus SettlementStatus,
        PaymentDistributionStatus DistributionStatus,
        string? OperatorId,
        string strawManId,
        DateTime CreatedAt,
        DateTime? PaidAt,
        DateTime? RefundedAt,
        DateTime? KilledAt,
        string? KillReason,
        DateTime? WithdrawnAt,
        DateTime? DistributedAt)
    {
        this.Id = Id;
        this.OperationId = OperationId;
        this.Gateway = Gateway;
        this.GatewayTransactionId = GatewayTransactionId;
        this.Amount = Amount;
        this.Splits = Splits;

        this.Status = Status;
        this.SettlementStatus = SettlementStatus;
        this.DistributionStatus = DistributionStatus;
        this.OperatorId = OperatorId;
        this.StrawManId = strawManId?.Trim() ?? string.Empty;

        this.CreatedAt = CreatedAt;
        this.PaidAt = PaidAt;
        this.RefundedAt = RefundedAt;
        this.KilledAt = KilledAt;
        this.KillReason = KillReason;
        this.WithdrawnAt = WithdrawnAt;
        this.DistributedAt = DistributedAt;
    }

    public IResult BindToStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                .WithMessage("O ID da conta laranja não pode estar vazio.")
                .Build());

        strawManId = strawManId.Trim();

        if (string.Equals(StrawManId, strawManId, StringComparison.Ordinal))
            return Result.Success();

        if (!string.IsNullOrWhiteSpace(StrawManId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManAlreadyBound)
                .WithMessage("Este pagamento já está vinculado a uma conta laranja.")
                .Build());

        StrawManId = strawManId;
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

        if (OperatorId is not null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorAlreadyBound)
                .WithMessage("Este pagamento já está vinculado a um operador.")
                .Build());

        OperatorId = operatorId;
        return Result.Success();
    }

    public IResult MarkAsPaid()
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidTransition)
                .WithMessage($"Não é possível marcar como pago a partir do status {DescribeStatus(Status)}.")
                .Build());

        if (string.IsNullOrWhiteSpace(OperatorId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorRequired)
                .WithMessage("É necessário vincular um operador antes de marcar o pagamento como pago.")
                .Build());

        if (string.IsNullOrWhiteSpace(StrawManId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManRequired)
                .WithMessage("É necessário vincular um laranja antes de marcar o pagamento como pago.")
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

    public IResult MarkAsDistributed()
    {
        if (DistributionStatus == PaymentDistributionStatus.Complete)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AlreadyDistributed)
                .WithMessage("Este pagamento já foi marcado como repassado às partes.")
                .Build());

        if (Status != PaymentStatus.Paid)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidDistributionTransition)
                .WithMessage($"Não é possível marcar repasse a partir do status {DescribeStatus(Status)}.")
                .Build());

        if (SettlementStatus != PaymentSettlementStatus.Withdrawn)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.DistributionRequiresWithdrawal)
                .WithMessage("O pagamento precisa estar sacado do gateway antes de marcar o repasse às partes.")
                .Build());

        if (DistributionStatus != PaymentDistributionStatus.Pending)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.InvalidDistributionTransition)
                .WithMessage($"Não é possível marcar repasse com distribuição {DescribeDistributionStatus(DistributionStatus)}.")
                .Build());

        DistributionStatus = PaymentDistributionStatus.Complete;
        DistributedAt = DateTime.UtcNow;
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

    public IResult Kill(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.KillReasonRequired)
                .WithMessage("O motivo do cancelamento é obrigatório.")
                .Build());

        if (Status == PaymentStatus.Killed)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AlreadyKilled)
                .WithMessage("Este pagamento já foi cancelado.")
                .Build());

        Status = PaymentStatus.Killed;
        KilledAt = DateTime.UtcNow;
        KillReason = reason;
        return Result.Success();
    }

    private static string DescribeStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "pendente",
        PaymentStatus.Paid => "pago",
        PaymentStatus.Refunded => "reembolsado",
        PaymentStatus.Killed => "cancelado",
        _ => status.ToString(),
    };

    private static string DescribeSettlementStatus(PaymentSettlementStatus status) => status switch
    {
        PaymentSettlementStatus.Unsettled => "pendente de saque",
        PaymentSettlementStatus.Withdrawn => "sacado",
        _ => status.ToString(),
    };

    private static string DescribeDistributionStatus(PaymentDistributionStatus status) => status switch
    {
        PaymentDistributionStatus.Pending => "pendente de repasse",
        PaymentDistributionStatus.Complete => "repassado",
        _ => status.ToString(),
    };
}
