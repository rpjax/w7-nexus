using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class GatewayCredentialsGroupRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public List<string> GatewayCredentialsIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
