using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class PixPaymentRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    // Aggregate identity (PixPayment.Id)
    public string PixPaymentId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;

    // Gateway references
    public int Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;

    // Payment details
    public decimal Amount { get; set; }
    public int Status { get; set; }

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