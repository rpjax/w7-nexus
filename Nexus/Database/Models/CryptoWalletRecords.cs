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
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class CryptoBalanceRecord
{
    public string Id { get; set; } = string.Empty;
    public Chain Chain { get; set; }
    public CryptoAsset Asset { get; set; }
    public decimal Amount { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CryptoBalanceSplitRecord> Splits { get; set; } = new();
    public List<string> AppliedStrawManFeeIds { get; set; } = new();
    public CryptoBalanceOriginRecord Origin { get; set; } = new();
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
    public List<CryptoBalanceRecord> Balances { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
