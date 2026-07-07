using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class StrawManSettingsRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StrawManId { get; set; } = string.Empty;
    public decimal MovementFeePercentage { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedByAdminId { get; set; } = string.Empty;
}
