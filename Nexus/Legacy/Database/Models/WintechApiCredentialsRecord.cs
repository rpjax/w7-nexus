using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Legacy.Database.Models;

public sealed class WintechApiCredentialsRecord
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string? StrawManId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Quando ausente no BSON, trata-se como habilitada (credenciais antigas).</summary>
    [BsonElement("enabled")]
    [BsonDefaultValue(true)]
    public bool Enabled { get; set; } = true;
}
