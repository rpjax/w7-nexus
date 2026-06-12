using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Infrastructure.Mapping;

internal static class TeamRecordMapping
{
    public static Team ToTeam(TeamRecord record) =>
        new(
            record.Id.ToString(),
            record.OperationId,
            record.Name,
            record.TeamLeaderId,
            record.OperatorIds,
            record.StrawManIds,
            record.GatewaySelectionStrategy,
            record.GatewayCredentialsIds,
            record.GatewayCredentialsGroupIds,
            record.OperatorProfitShareRules,
            record.CreatedAt,
            record.UpdatedAt);

    public static TeamRecord ToRecord(Team team)
    {
        return new TeamRecord
        {
            Id = string.IsNullOrWhiteSpace(team.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(team.Id),
            OperationId = team.OperationId,
            Name = team.Name,
            TeamLeaderId = team.TeamLeaderId,
            OperatorIds = team.OperatorIds.ToList(),
            StrawManIds = team.StrawManIds.ToList(),
            GatewaySelectionStrategy = team.GatewaySelectionStrategy,
            GatewayCredentialsIds = team.GatewayCredentialsIds.ToList(),
            GatewayCredentialsGroupIds = team.GatewayCredentialsGroupIds.ToList(),
            OperatorProfitShareRules = team.OperatorProfitShareRules.ToList(),
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }
}
