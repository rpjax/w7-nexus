using Nexus.Payments.Aggregates;

namespace Nexus.Tests.Payments;

internal static class PaymentTestFactory
{
    public static Payment Create(
        string? id = null,
        string operationId = "operation-1",
        PaymentGateway gateway = PaymentGateway.FusionPay,
        string gatewayPaymentId = "gw-1",
        decimal amount = 10m,
        IReadOnlyList<PaymentSplit>? splits = null,
        PaymentStatus status = PaymentStatus.Pending,
        PaymentSettlementStatus settlementStatus = PaymentSettlementStatus.Unsettled,
        PaymentDistributionStatus distributionStatus = PaymentDistributionStatus.Pending,
        string? operatorId = null,
        string strawManId = "sm-1",
        DateTime? createdAt = null,
        DateTime? paidAt = null,
        DateTime? refundedAt = null,
        DateTime? killedAt = null,
        string? killReason = null,
        DateTime? withdrawnAt = null,
        DateTime? distributedAt = null) =>
        new(
            id ?? Guid.NewGuid().ToString("N"),
            operationId,
            gateway,
            gatewayPaymentId,
            amount,
            splits ?? Array.Empty<PaymentSplit>(),
            status,
            settlementStatus,
            distributionStatus,
            operatorId,
            strawManId,
            createdAt ?? DateTime.UtcNow,
            paidAt,
            refundedAt,
            killedAt,
            killReason,
            withdrawnAt,
            distributedAt);
}
