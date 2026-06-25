using MongoDB.Bson.Serialization.Attributes;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.Database.Models;

public sealed class BankBalanceDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string BankAccountId { get; set; } = string.Empty;
    public decimal AmountBrl { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BankBalanceSplitRecord> Splits { get; set; } = new();
    public BankBalanceOriginRecord Origin { get; set; } = new();
}
