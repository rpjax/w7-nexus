using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.Payments.Aggregates;

namespace Nexus.Database.Models;

public sealed class PaymentRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OperationId { get; set; } = string.Empty;

    public PaymentGateway Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public List<PaymentSplitRecord> Splits { get; set; } = new();
    public PaymentSettlementStatus SettlementStatus { get; set; }
    public PaymentDistributionStatus DistributionStatus { get; set; }

    public string? OperatorId { get; set; }
    public string StrawManId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? KilledAt { get; set; }
    public string? KillReason { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? DistributedAt { get; set; }
}
