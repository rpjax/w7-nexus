using MongoDB.Bson.Serialization.Attributes;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.Database.Models;

public sealed class CryptoBalanceDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string CryptoWalletId { get; set; } = string.Empty;
    public Chain Chain { get; set; }
    public CryptoAsset Asset { get; set; }
    public decimal Amount { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CryptoBalanceSplitRecord> Splits { get; set; } = new();
    public CryptoBalanceOriginRecord Origin { get; set; } = new();
}
