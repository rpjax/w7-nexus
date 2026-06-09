using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Legacy.Database.Models;

public sealed class OperationRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OperationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Operators { get; set; } = new();
    public List<string> StrawManIds { get; set; } = new();
    public bool ManuallySetChargeCredentials { get; set; }
    public List<string> ChargeCredentialsIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
