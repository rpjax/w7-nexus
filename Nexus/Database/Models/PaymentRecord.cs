using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.Payments.Aggregates;

namespace Nexus.Database.Models;

public sealed class PaymentRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    // Aggregate identity (PixPayment.Id)
    public string PixPaymentId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;

    // Gateway references
    public PaymentGateway Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;

    // Payment details
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }

    // Binding details
    public string? OperatorAccountId { get; set; }
    public string? StrawManAccountId { get; set; }

    // Timestamps & additional info
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? DiedAt { get; set; }
    public string? DeathReason { get; set; }
}