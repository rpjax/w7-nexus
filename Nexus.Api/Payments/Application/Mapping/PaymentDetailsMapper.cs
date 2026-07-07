using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Mapping;

public static class PaymentDetailsMapper
{
    public static PaymentDetails Map(Payment payment) => new()
    {
        Id = payment.Id,
        OperationId = payment.OperationId,
        OperatorId = payment.OperatorId,
        StrawManId = payment.StrawManId,
        Gateway = payment.Gateway.ToString(),
        GatewayTransactionId = payment.GatewayTransactionId,
        Amount = payment.Amount,
        Splits = payment.Splits
            .Select(split => new PaymentSplitDetails
            {
                AccountId = split.AccountId,
                Percentage = split.Percentage,
                Amount = split.Amount,
            })
            .ToArray(),
        Status = payment.Status.ToString(),
        SettlementStatus = payment.SettlementStatus.ToString(),
        DistributionStatus = payment.DistributionStatus.ToString(),
        CreatedAt = payment.CreatedAt,
        PaidAt = payment.PaidAt,
        RefundedAt = payment.RefundedAt,
        KilledAt = payment.KilledAt,
        KillReason = payment.KillReason,
        WithdrawnAt = payment.WithdrawnAt,
        DistributedAt = payment.DistributedAt,
    };

    public static IReadOnlyList<PaymentDetails> MapMany(IEnumerable<Payment> payments) =>
        payments.Select(Map).ToArray();
}
