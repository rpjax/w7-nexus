using Nexus.Payments.Aggregates;

namespace Nexus.Tests.Payments;

internal static class PaymentTestFactory
{
    public static Payment Create(
        string? id = null,
        string operationId = "operation-1",
        string teamId = "",
        PaymentGateway gateway = PaymentGateway.FusionPay,
        string gatewayPaymentId = "gw-1",
        decimal amount = 10m,
        IReadOnlyList<PaymentSplit>? splits = null,
        PaymentStatus status = PaymentStatus.Pending,
        PaymentSettlementStatus settlementStatus = PaymentSettlementStatus.Unsettled,
        string? operatorAccountId = null,
        string? strawManAccountId = null,
        DateTime? createdAt = null,
        DateTime? paidAt = null,
        DateTime? refundedAt = null,
        DateTime? diedAt = null,
        string? deathReason = null,
        DateTime? withdrawnAt = null) =>
        new(
            id ?? Guid.NewGuid().ToString("N"),
            operationId,
            teamId,
            gateway,
            gatewayPaymentId,
            amount,
            splits ?? Array.Empty<PaymentSplit>(),
            status,
            settlementStatus,
            operatorAccountId,
            strawManAccountId,
            createdAt ?? DateTime.UtcNow,
            paidAt,
            refundedAt,
            diedAt,
            deathReason,
            withdrawnAt);
}
