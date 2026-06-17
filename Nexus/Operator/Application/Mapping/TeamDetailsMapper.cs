using Nexus.Database.Models;
using Nexus.Operator.Application.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operator.Application.Mapping;

public static class TeamDetailsMapper
{
    public static TeamDetails Map(
        Team team,
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        string viewerOperatorAccountId)
    {
        return new TeamDetails
        {
            Id = team.Id,
            OperationId = team.OperationId,
            Name = team.Name,
            TeamLeader = string.IsNullOrWhiteSpace(team.TeamLeaderId)
                ? null
                : new TeamLeaderDetails
                {
                    AccountId = team.TeamLeaderId,
                    Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, team.TeamLeaderId),
                },
            Operators = team.OperatorIds
                .Select(operatorId => MapOperator(operatorId, usernamesByAccountId))
                .ToArray(),
            ProfitShareRule = MapViewerProfitShareRule(team, viewerOperatorAccountId, usernamesByAccountId),
        };
    }

    private static OperatorDetails MapOperator(
        string operatorId,
        IReadOnlyDictionary<string, string> usernamesByAccountId)
    {
        return new OperatorDetails
        {
            AccountId = operatorId,
            Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, operatorId),
        };
    }

    private static ProfitShareRuleDetails MapViewerProfitShareRule(
        Team team,
        string viewerOperatorAccountId,
        IReadOnlyDictionary<string, string> usernamesByAccountId)
    {
        var rule = team.OperatorProfitShareRules
            .FirstOrDefault(r => string.Equals(r.OperatorId, viewerOperatorAccountId, StringComparison.Ordinal));

        return new ProfitShareRuleDetails
        {
            Cuts = (rule?.Cuts ?? new List<ProfitSplitRecord>())
                .Select(cut => new ProfitSplitDetails
                {
                    AccountId = cut.AccountId,
                    Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, cut.AccountId),
                    Percentage = cut.Percentage,
                })
                .ToArray(),
        };
    }
}
