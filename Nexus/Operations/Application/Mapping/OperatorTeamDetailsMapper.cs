using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Responses.Operator.Models;

namespace Nexus.Operations.Application.Mapping;

public static class OperatorTeamDetailsMapper
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
                    Username = OperatorOperationDetailsMapper.ResolveUsername(usernamesByAccountId, team.TeamLeaderId),
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
            Username = OperatorOperationDetailsMapper.ResolveUsername(usernamesByAccountId, operatorId),
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
                    Username = OperatorOperationDetailsMapper.ResolveUsername(usernamesByAccountId, cut.AccountId),
                    Percentage = cut.Percentage,
                })
                .ToArray(),
        };
    }
}
