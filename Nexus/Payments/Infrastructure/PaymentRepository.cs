using Aidan.Mongo.Linq;
using Aidan.Core.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;

namespace Nexus.Payments.Infrastructure;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<PaymentRecord> _collection;

    private static readonly Expression<Func<PaymentRecord, Payment>> ToPixPaymentProjection = r =>
        new Payment(
            r.PixPaymentId,
            r.OperationId,
            r.Gateway,
            r.GatewayPaymentId,
            r.Amount,
            r.Status,
            r.OperatorAccountId,
            r.StrawManAccountId,
            r.CreatedAt,
            r.PaidAt,
            r.RefundedAt,
            r.DiedAt,
            r.DeathReason);

    public PaymentRepository(IMongoCollection<PaymentRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Payment> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToPixPaymentProjection);
        return new MongoAsyncQueryable<Payment>(source);
    }

    public Task CreateAsync(Payment entity)
    {
        var record = ToRecord(entity, bsonId: null);
        return _collection.InsertOneAsync(record);
    }

    public Task CreateAsync(IEnumerable<Payment> entities)
    {
        var records = entities.Select(e => ToRecord(e, bsonId: null));
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Payment entity)
    {
        return _collection.DeleteOneAsync(r => r.PixPaymentId == entity.Id);
    }

    public async Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate)
    {
        var paymentsToDelete = AsQueryable().Where(predicate).ToList();
        if (paymentsToDelete.Count == 0)
            return 0;

        var ids = paymentsToDelete.Select(p => p.Id).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.PixPaymentId));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(Payment entity)
    {
        var existing = await _collection.Find(r => r.PixPaymentId == entity.Id).FirstOrDefaultAsync();
        if (existing is null)
            throw new InvalidOperationException($"Payment '{entity.Id}' was not found for update.");

        var record = ToRecord(entity, existing.Id);
        await _collection.ReplaceOneAsync(r => r.PixPaymentId == entity.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(PixPayment) instead.");
    }

    private static PaymentRecord ToRecord(Payment entity, ObjectId? bsonId)
    {
        return new PaymentRecord
        {
            Id = bsonId ?? ObjectId.GenerateNewId(),
            PixPaymentId = entity.Id,
            OperationId = entity.OperationId,
            Gateway = entity.Gateway,
            GatewayPaymentId = entity.GatewayTransactionId,
            Amount = entity.Amount,
            Status = entity.Status,
            OperatorAccountId = entity.OperatorAccountId,
            StrawManAccountId = entity.StrawManAccountId,
            CreatedAt = entity.CreatedAt,
            PaidAt = entity.PaidAt,
            RefundedAt = entity.RefundedAt,
            DiedAt = entity.DiedAt,
            DeathReason = entity.DeathReason
        };
    }
}
