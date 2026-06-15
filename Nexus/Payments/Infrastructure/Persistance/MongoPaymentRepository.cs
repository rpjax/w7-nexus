using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Infrastructure.Mapping;

namespace Nexus.Payments.Infrastructure.Persistance;

public sealed class MongoPaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<PaymentRecord> _collection;

    private static readonly Expression<Func<PaymentRecord, Payment>> ToPaymentProjection = r =>
        new Payment(
            r.Id.ToString(),
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

    public MongoPaymentRepository(IMongoCollection<PaymentRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Payment> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToPaymentProjection);
        return new MongoAsyncQueryable<Payment>(source);
    }

    public async Task<Payment> CreateAsync(Payment entity)
    {
        var record = PaymentRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return PaymentRecordMapping.ToPayment(record);
    }

    async Task IRepository<Payment>.CreateAsync(Payment entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<Payment> entities)
    {
        var records = entities.Select(e => PaymentRecordMapping.ToRecord(e));
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Payment entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate)
    {
        var paymentsToDelete = AsQueryable().Where(predicate).ToList();
        if (paymentsToDelete.Count == 0)
            return 0;

        var objectIds = paymentsToDelete.Select(p => ObjectId.Parse(p.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(Payment entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var record = PaymentRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Payment) instead.");
    }
}
