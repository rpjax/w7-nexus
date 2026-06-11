using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;

namespace Nexus.Operations.Infrastructure;

public sealed class TeamRepository : ITeamRepository
{
    private readonly IMongoCollection<TeamRecord> _collection;

    private static readonly Expression<Func<TeamRecord, Team>> ToTeamProjection = r =>
        new Team(
            r.TeamId,
            r.OperationId,
            r.Name,
            r.TeamLeaderId,
            r.Operators,
            r.StrawManIds,
            r.GatewaySelectionStrategy,
            r.GatewayCredentialsIds,
            r.GatewayCredentialsGroupIds,
            r.OperatorProfitShareRules,
            r.CreatedAt,
            r.UpdatedAt);

    public TeamRepository(IMongoCollection<TeamRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Team> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToTeamProjection);
        return new MongoAsyncQueryable<Team>(source);
    }

    public Task CreateAsync(Team entity)
    {
        var record = ToRecord(entity);
        return _collection.InsertOneAsync(record);
    }

    public Task CreateAsync(IEnumerable<Team> entities)
    {
        var records = entities.Select(ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Team entity)
    {
        return _collection.DeleteOneAsync(r => r.TeamId == entity.Id);
    }

    public async Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(t => t.Id).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.TeamId));
        return result.DeletedCount;
    }

    public Task UpdateAsync(Team entity)
    {
        var update = Builders<TeamRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.TeamLeaderId, entity.TeamLeaderId)
            .Set(r => r.Operators, entity.OperatorIds.ToList())
            .Set(r => r.StrawManIds, entity.StrawManIds.ToList())
            .Set(r => r.GatewaySelectionStrategy, (int)entity.GatewaySelectionStrategy)
            .Set(r => r.GatewayCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.GatewayCredentialsGroupIds, entity.GatewayCredentialsGroupIds.ToList())
            .Set(r => r.OperatorProfitShareRules, ToProfitShareRuleRecords(entity))
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.TeamId == entity.Id, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Team) instead.");
    }

    private static TeamRecord ToRecord(Team team)
    {
        return new TeamRecord
        {
            Id = ObjectId.GenerateNewId(),
            TeamId = team.Id,
            OperationId = team.OperationId,
            Name = team.Name,
            TeamLeaderId = team.TeamLeaderId,
            Operators = team.OperatorIds.ToList(),
            StrawManIds = team.StrawManIds.ToList(),
            GatewaySelectionStrategy = (int)team.GatewaySelectionStrategy,
            GatewayCredentialsIds = team.GatewayCredentialsIds.ToList(),
            GatewayCredentialsGroupIds = team.GatewayCredentialsGroupIds.ToList(),
            OperatorProfitShareRules = ToProfitShareRuleRecords(team),
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }

    private static List<OperatorProfitShareRuleRecord> ToProfitShareRuleRecords(Team team)
        => team.OperatorProfitShareRules.Values
            .Select(rule => new OperatorProfitShareRuleRecord
            {
                OperatorId = rule.OperatorId,
                Cuts = rule.ProfitSplits.Values
                    .Select(cut => new ProfitSplitRecord
                    {
                        AccountId = cut.AccountId,
                        Percentage = cut.Percentage
                    })
                    .ToList()
            })
            .ToList();
}
