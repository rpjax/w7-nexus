using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.AccountNodes.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Database.Models;

public sealed class TransferProofRecord
{
    public string? PixTransactionId { get; set; }
    public string? PixAuthenticationCode { get; set; }
    public string? CryptoTransactionId { get; set; }
}

public sealed class AccountNodeSnapshotRecord
{
    public AccountNodeKind Kind { get; set; }
    public string? BankAccountId { get; set; }
    public string? CryptoWalletId { get; set; }
    public string? ParticipantAccountId { get; set; }
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class TransferRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public TransferType Type { get; set; }
    public OnrampingMethod? OnrampingMethod { get; set; }
    public TransferProofRecord? Proof { get; set; }
    public AccountNodeSnapshotRecord? Source { get; set; }
    public AccountNodeSnapshotRecord? Destination { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal? ProducedAmount { get; set; }
    public CryptoAsset? ProducedAsset { get; set; }
    public List<string> PaymentIds { get; set; } = new();
    public string? SourceBalanceId { get; set; }
    public Chain? ProducedChain { get; set; }
    public string StrawManId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
