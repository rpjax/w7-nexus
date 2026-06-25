using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.Database.Models;

public sealed class CryptoBalanceSplitRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public CryptoSplitKind SplitKind { get; set; }
}

public sealed class CryptoBalanceOriginRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
}

public sealed class CryptoWalletAddressRecord
{
    public AddressNamespace Namespace { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Memo { get; set; }
}

public sealed class CryptoWalletRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public List<CryptoWalletAddressRecord> Addresses { get; set; } = new();
    public string? Label { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
