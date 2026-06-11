using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Infrastructure.Mapping;

internal static class PaymentRecordMapping
{
    public static Payment ToPayment(PaymentRecord record) =>
        new(
            record.PixPaymentId,
            record.OperationId,
            record.Gateway,
            record.GatewayPaymentId,
            record.Amount,
            record.Status,
            record.OperatorAccountId,
            record.StrawManAccountId,
            record.CreatedAt,
            record.PaidAt,
            record.RefundedAt,
            record.DiedAt,
            record.DeathReason);

    public static PaymentRecord ToRecord(Payment entity, ObjectId? existingBsonId = null)
    {
        var paymentId = string.IsNullOrWhiteSpace(entity.Id)
            ? Guid.NewGuid().ToString("N")
            : entity.Id;

        return new PaymentRecord
        {
            Id = existingBsonId ?? ObjectId.GenerateNewId(),
            PixPaymentId = paymentId,
            OperationId = entity.OperationId,
            Gateway = entity.Gateway,
            GatewayPaymentId = entity.GatewayTransactionId,
            Amount = entity.Amount,
            Status = entity.Status,
            OperatorAccountId = entity.OperatorAccountId,
            StrawManAccountId = entity.StrawManAccountId,
            CreatedAt = entity.CreatedAt,
            PaidAt = entity.PaidAt,
            RefundedAt = entity.RefundedAt,
            DiedAt = entity.DiedAt,
            DeathReason = entity.DeathReason
        };
    }
}
