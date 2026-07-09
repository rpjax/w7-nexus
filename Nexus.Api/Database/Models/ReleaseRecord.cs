using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class ReleaseRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string ScriptId { get; set; } = string.Empty;
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public bool IsDeprecated { get; set; }
    public DateTime CreatedAt { get; set; }
}
