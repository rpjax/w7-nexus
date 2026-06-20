using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.Withdrawals.Aggregates;

namespace Nexus.Database.Models;

public sealed class PixProofRecord
{
    public string? TransactionId { get; set; }
    public string? AuthenticationCode { get; set; }
}

public sealed class CryptoProofRecord
{
    public string? TransactionId { get; set; }
}

public sealed class BankAccountRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StrawManAccountId { get; set; } = string.Empty;
    public BrazilianBank Bank { get; set; }
    public string Agency { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? AccountDigit { get; set; }
    public BankAccountType AccountType { get; set; }
    public string? PixKey { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CryptoWalletRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StrawManAccountId { get; set; } = string.Empty;
    public Chain Chain { get; set; }
    public CryptoAsset Asset { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class WithdrawalRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string OperationId { get; set; } = string.Empty;
    public WithdrawalType Type { get; set; }
    public string StrawManAccountId { get; set; } = string.Empty;
    public string? BankAccountId { get; set; }
    public string? CryptoWalletId { get; set; }
    public List<string> PaymentIds { get; set; } = new();
    public string? CostDescription { get; set; }
    public decimal CostAmount { get; set; }
    public PixProofRecord? PixProof { get; set; }
    public CryptoProofRecord? CryptoProof { get; set; }
    public decimal PaymentsTotalAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
