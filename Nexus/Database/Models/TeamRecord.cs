using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Database.Models;

public sealed class TeamRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string TeamId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TeamLeaderId { get; set; }
    public List<string> Operators { get; set; } = new();
    public List<string> StrawManIds { get; set; } = new();
    public int GatewaySelectionStrategy { get; set; }
    public List<string> ChargeCredentialsIds { get; set; } = new();
    public List<string> GatewayCredentialsGroupIds { get; set; } = new();
    public List<OperatorProfitShareRuleRecord> OperatorProfitShareRules { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
