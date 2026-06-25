using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Infrastructure.Mapping;

namespace Nexus.Transfers.Infrastructure.Persistance;

public sealed class MongoTransferRepository : ITransferRepository
{
    private readonly IMongoCollection<TransferRecord> _collection;

    private static readonly Expression<Func<TransferRecord, Transfer>> ToProjection = r =>
        new Transfer(
            r.Id.ToString(),
            r.Type,
            r.OnrampingMethod,
            r.Proof == null
                || (r.Proof.PixTransactionId == null
                    && r.Proof.PixAuthenticationCode == null
                    && r.Proof.CryptoTransactionId == null)
                ? null
                : new TransferProof(
                    r.Proof.PixTransactionId,
                    r.Proof.PixAuthenticationCode,
                    r.Proof.CryptoTransactionId),
            r.OriginType,
            r.OriginBankAccount == null
                ? null
                : new TransferOriginBankAccount(r.OriginBankAccount.BankAccountId, r.OriginBankAccount.StrawManId),
            r.OriginCryptoWallet == null
                ? null
                : new TransferOriginCryptoWallet(r.OriginCryptoWallet.CryptoWalletId, r.OriginCryptoWallet.StrawManId),
            r.DestinationType,
            r.DestinationBankAccount == null
                ? null
                : new TransferDestinationBankAccount(
                    r.DestinationBankAccount.BankAccountId,
                    r.DestinationBankAccount.StrawManId),
            r.DestinationCryptoWallet == null
                ? null
                : new TransferDestinationCryptoWallet(
                    r.DestinationCryptoWallet.CryptoWalletId,
                    r.DestinationCryptoWallet.StrawManId),
            r.SourceAmount,
            r.ProducedAmount,
            r.ProducedAsset,
            r.ProducedChain,
            r.PaymentIds,
            r.SourceBalanceId,
            r.StrawManId,
            r.CreatedAt);

    public MongoTransferRepository(IMongoCollection<TransferRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Transfer> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<Transfer>(source);
    }

    public async Task<Transfer> CreateAsync(Transfer entity)
    {
        var record = TransferRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return TransferRecordMapping.ToTransfer(record);
    }

    async Task IRepository<Transfer>.CreateAsync(Transfer entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<Transfer> entities)
    {
        var records = entities.Select(TransferRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Transfer entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Transfer, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(t => ObjectId.Parse(t.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(Transfer entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var record = TransferRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Transfer) instead.");
}
