using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services.Contracts;
using Nexus.Operations.Infrastructure.Mapping;

namespace Nexus.Operations.Infrastructure.Persistance;

public sealed class MongoTeamRepository : ITeamRepository
{
    private readonly IMongoCollection<TeamRecord> _collection;

    private static readonly Expression<Func<TeamRecord, Team>> ToTeamProjection = r =>
        new Team(
            r.Id.ToString(),
            r.OperationId,
            r.Name,
            r.TeamLeaderId,
            r.OperatorIds,
            r.StrawManIds,
            r.GatewaySelectionStrategy,
            r.GatewayCredentialsIds,
            r.GatewayCredentialsGroupIds,
            r.OperatorProfitShareRules,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoTeamRepository(IMongoCollection<TeamRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Team> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToTeamProjection);
        return new MongoAsyncQueryable<Team>(source);
    }

    public async Task<Team> CreateAsync(Team entity)
    {
        var record = TeamRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return TeamRecordMapping.ToTeam(record);
    }

    async Task IRepository<Team>.CreateAsync(Team entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<Team> entities)
    {
        var records = entities.Select(TeamRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Team entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(t => ObjectId.Parse(t.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public Task UpdateAsync(Team entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var update = Builders<TeamRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.TeamLeaderId, entity.TeamLeaderId)
            .Set(r => r.OperatorIds, entity.OperatorIds.ToList())
            .Set(r => r.StrawManIds, entity.StrawManIds.ToList())
            .Set(r => r.GatewaySelectionStrategy, entity.GatewaySelectionStrategy)
            .Set(r => r.GatewayCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.GatewayCredentialsGroupIds, entity.GatewayCredentialsGroupIds.ToList())
            .Set(r => r.OperatorProfitShareRules, entity.OperatorProfitShareRules.ToList())
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.Id == objectId, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Team) instead.");
    }
}
