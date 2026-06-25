using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nexus.CryptoWallets.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Database.Models;

public sealed class TransferProofRecord
{
    public string? PixTransactionId { get; set; }
    public string? PixAuthenticationCode { get; set; }
    public string? CryptoTransactionId { get; set; }
}

public sealed class TransferOriginBankAccountRecord
{
    public string BankAccountId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class TransferOriginCryptoWalletRecord
{
    public string CryptoWalletId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class TransferDestinationBankAccountRecord
{
    public string BankAccountId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class TransferDestinationCryptoWalletRecord
{
    public string CryptoWalletId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

public sealed class TransferRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public TransferType Type { get; set; }
    public OnrampingMethod? OnrampingMethod { get; set; }
    public TransferProofRecord? Proof { get; set; }
    public TransferOriginType? OriginType { get; set; }
    public TransferOriginBankAccountRecord? OriginBankAccount { get; set; }
    public TransferOriginCryptoWalletRecord? OriginCryptoWallet { get; set; }
    public TransferDestinationType? DestinationType { get; set; }
    public TransferDestinationBankAccountRecord? DestinationBankAccount { get; set; }
    public TransferDestinationCryptoWalletRecord? DestinationCryptoWallet { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal? ProducedAmount { get; set; }
    public CryptoAsset? ProducedAsset { get; set; }
    public List<string> PaymentIds { get; set; } = new();
    public string? SourceBalanceId { get; set; }
    public Chain? ProducedChain { get; set; }
    public string StrawManId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
