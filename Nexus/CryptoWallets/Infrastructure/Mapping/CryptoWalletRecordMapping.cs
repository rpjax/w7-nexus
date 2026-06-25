using MongoDB.Bson;
using Nexus.CryptoWallets.Aggregates;
using Nexus.Database.Models;

namespace Nexus.CryptoWallets.Infrastructure.Mapping;

internal static class CryptoWalletRecordMapping
{
    public static CryptoWallet ToCryptoWallet(CryptoWalletRecord record) =>
        new(
            record.Id.ToString(),
            record.OwnerId,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt,
            record.Addresses.Select(ToAddress).ToList());

    public static CryptoWalletRecord ToRecord(CryptoWallet entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            OwnerId = entity.OwnerId,
            Addresses = entity.Addresses.Select(ToRecord).ToList(),
            Label = entity.Label,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

    private static CryptoWalletAddress ToAddress(CryptoWalletAddressRecord record) =>
        CryptoWalletAddress.Create(record.Namespace, record.Address, record.Memo).Value!;

    private static CryptoWalletAddressRecord ToRecord(CryptoWalletAddress address) =>
        new()
        {
            Namespace = address.Namespace,
            Address = address.Address,
            Memo = address.Memo,
        };
}
