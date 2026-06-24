using MongoDB.Bson;
using Nexus.Database;
using Nexus.Database.Models;
using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Infrastructure.Mapping;

internal static class PaymentRecordMapping
{
    public static Payment ToPayment(PaymentRecord record) =>
        new(
            record.Id.ToString(),
            record.OperationId,
            record.Gateway,
            record.GatewayPaymentId,
            record.Amount,
            MapSplits(record.Splits),
            record.Status,
            record.SettlementStatus,
            record.OperatorId,
            record.StrawManId ?? string.Empty,
            record.CreatedAt,
            record.PaidAt,
            record.RefundedAt,
            record.KilledAt,
            record.KillReason,
            record.WithdrawnAt);

    public static PaymentRecord ToRecord(Payment entity)
    {
        return new PaymentRecord
        {
            Id = MongoObjectIds.Resolve(entity.Id),
            OperationId = entity.OperationId,
            Gateway = entity.Gateway,
            GatewayPaymentId = entity.GatewayTransactionId,
            Amount = entity.Amount,
            Splits = entity.Splits.Select(split => new PaymentSplitRecord
            {
                AccountId = split.AccountId,
                Percentage = split.Percentage,
                Amount = split.Amount,
            }).ToList(),
            Status = entity.Status,
            SettlementStatus = entity.SettlementStatus,
            OperatorId = entity.OperatorId,
            StrawManId = entity.StrawManId,
            CreatedAt = entity.CreatedAt,
            PaidAt = entity.PaidAt,
            RefundedAt = entity.RefundedAt,
            KilledAt = entity.KilledAt,
            KillReason = entity.KillReason,
            WithdrawnAt = entity.WithdrawnAt,
        };
    }

    private static IReadOnlyList<PaymentSplit> MapSplits(IReadOnlyList<PaymentSplitRecord>? splits)
    {
        if (splits is null || splits.Count == 0)
            return Array.Empty<PaymentSplit>();

        return splits
            .Select(split => new PaymentSplit(split.AccountId, split.Percentage, split.Amount))
            .ToList();
    }
}
