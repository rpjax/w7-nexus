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
            record.TeamId,
            record.Gateway,
            record.GatewayPaymentId,
            record.Amount,
            MapSplits(record.Splits),
            record.Status,
            record.SettlementStatus,
            record.OperatorAccountId,
            record.StrawManAccountId,
            record.CreatedAt,
            record.PaidAt,
            record.RefundedAt,
            record.DiedAt,
            record.DeathReason,
            record.WithdrawnAt);

    public static PaymentRecord ToRecord(Payment entity)
    {
        return new PaymentRecord
        {
            Id = MongoObjectIds.Resolve(entity.Id),
            OperationId = entity.OperationId,
            TeamId = entity.TeamId,
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
            OperatorAccountId = entity.OperatorAccountId,
            StrawManAccountId = entity.StrawManAccountId,
            CreatedAt = entity.CreatedAt,
            PaidAt = entity.PaidAt,
            RefundedAt = entity.RefundedAt,
            DiedAt = entity.DiedAt,
            DeathReason = entity.DeathReason,
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
