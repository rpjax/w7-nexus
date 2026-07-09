using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.Scripts.Aggregates;

namespace Nexus.Database.Models;

public sealed class ScriptRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public List<string> HostPatterns { get; set; } = new();
    public int Priority { get; set; }
    public string? Description { get; set; }
    public List<ChannelRecord> Channels { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ChannelRecord
{
    public ObjectId Id { get; set; }
    public ChannelType Type { get; set; }
    public string? CustomName { get; set; }
    public string? CurrentReleaseId { get; set; }
}
