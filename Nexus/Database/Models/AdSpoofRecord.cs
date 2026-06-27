using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class AdSpoofRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OperationId { get; set; } = string.Empty;
    public string AdId { get; set; } = string.Empty;
    public string AdUrl { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public bool IsImpersonating { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
