using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.AccountNodes.Aggregates;

namespace Nexus.Database.Models;

public sealed class BalanceSplitSnapshotRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public SplitKind SplitKind { get; set; }
}

public sealed class BalanceOriginSnapshotRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class BankBalanceRecord
{
    public string Id { get; set; } = string.Empty;
    public decimal AmountBrl { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BalanceSplitSnapshotRecord> SplitSnapshot { get; set; } = new();
    public List<string> AppliedStrawManFeeIds { get; set; } = new();
    public BalanceOriginSnapshotRecord OriginSnapshot { get; set; } = new();
}

public sealed class CryptoBalanceRecord
{
    public string Id { get; set; } = string.Empty;
    public Chain Chain { get; set; }
    public CryptoAsset Asset { get; set; }
    public decimal Amount { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BalanceSplitSnapshotRecord> SplitSnapshot { get; set; } = new();
    public List<string> AppliedStrawManFeeIds { get; set; } = new();
    public BalanceOriginSnapshotRecord OriginSnapshot { get; set; } = new();
}

public sealed class CryptoWalletAddressRecord
{
    public AddressNamespace Namespace { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Memo { get; set; }
}

public sealed class AccountNodeBankAccountRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StrawManId { get; set; } = string.Empty;
    public BrazilianBank Bank { get; set; }
    public string Agency { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? AccountDigit { get; set; }
    public BankAccountType AccountType { get; set; }
    public string? Label { get; set; }

    public List<BankBalanceRecord> Balances { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AccountNodeCryptoWalletRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StrawManId { get; set; } = string.Empty;
    public List<CryptoWalletAddressRecord> Addresses { get; set; } = new();
    public string? Label { get; set; }
    public List<CryptoBalanceRecord> Balances { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
