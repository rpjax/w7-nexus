using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Infrastructure.Mapping;

internal static class TeamRecordMapping
{
    public static Team ToTeam(TeamRecord record) =>
        new(
            record.TeamId,
            record.OperationId,
            record.Name,
            record.TeamLeaderId,
            record.Operators,
            record.StrawManIds,
            record.GatewaySelectionStrategy,
            record.GatewayCredentialsIds,
            record.GatewayCredentialsGroupIds,
            record.OperatorProfitShareRules,
            record.CreatedAt,
            record.UpdatedAt);

    public static TeamRecord ToRecord(Team team)
    {
        var teamId = string.IsNullOrWhiteSpace(team.Id)
            ? Guid.NewGuid().ToString("N")
            : team.Id;

        return new TeamRecord
        {
            Id = ObjectId.GenerateNewId(),
            TeamId = teamId,
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

    public static List<OperatorProfitShareRuleRecord> ToProfitShareRuleRecords(Team team)
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
