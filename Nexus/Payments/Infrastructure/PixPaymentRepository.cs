using Aidan.Mongo.Linq;
using Aidan.Core.Linq;
using System.Linq.Expressions;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Database.Models;
using MongoDB.Driver;

namespace Nexus.Payments.Infrastructure;

public sealed class PixPaymentRepository : IPixPaymentRepository
{
    private readonly IMongoCollection<PixPaymentRecord> _collection;

    private static readonly Expression<Func<PixPaymentRecord, PixPayment>> ToPixPaymentProjection = r =>
        new PixPayment(
            r.PixPaymentId,
            r.OperationId,
            (PaymentGateway)r.Gateway,
            r.GatewayPaymentId,
            r.Amount,
            (PaymentStatus)r.Status,
            r.OperatorAccountId,
            r.StrawManAccountId,
            r.CreatedAt,
            r.PaidAt,
            r.RefundedAt,
            r.DiedAt,
            r.DeathReason);

    public PixPaymentRepository(IMongoCollection<PixPaymentRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<PixPayment> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToPixPaymentProjection);
        return new MongoAsyncQueryable<PixPayment>(source);
    }

    public Task CreateAsync(PixPayment entity)
    {
        var record = ToRecord(entity);
        return _collection.InsertOneAsync(record);
    }

    public Task CreateAsync(IEnumerable<PixPayment> entities)
    {
        var records = entities.Select(ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(PixPayment entity)
    {
        return _collection.DeleteOneAsync(r => r.PixPaymentId == entity.Id);
    }

    public async Task<long> DeleteAsync(Expression<Func<PixPayment, bool>> predicate)
    {
        var paymentsToDelete = AsQueryable().Where(predicate).ToList();
        if (paymentsToDelete.Count == 0)
            return 0;

        var ids = paymentsToDelete.Select(p => p.Id).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.PixPaymentId));
        return result.DeletedCount;
    }

    public Task UpdateAsync(PixPayment entity)
    {
        var record = ToRecord(entity);
        return _collection.ReplaceOneAsync(r => r.PixPaymentId == entity.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(PixPayment) instead.");
    }

    private static PixPaymentRecord ToRecord(PixPayment entity)
    {
        return new PixPaymentRecord
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId(),
            PixPaymentId = entity.Id,
            OperationId = entity.OperationId,
            Gateway = (int)entity.Gateway,
            GatewayPaymentId = entity.GatewayPaymentId,
            Amount = entity.Amount,
            Status = (int)entity.Status,
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
