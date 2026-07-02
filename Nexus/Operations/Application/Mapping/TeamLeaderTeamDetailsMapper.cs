using Aidan.Core.Linq.Extensions;
using Nexus.Accounts.Application.Contracts;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Responses.TeamLeader.Models;

namespace Nexus.Operations.Application.Mapping;

public static class TeamLeaderTeamDetailsMapper
{
    public static TeamDetails Map(
        Team team,
        IReadOnlyDictionary<string, string> usernamesByAccountId)
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
                    Username = ResolveUsername(usernamesByAccountId, team.TeamLeaderId),
                },
            Operators = team.OperatorIds
                .Select(operatorId => MapOperator(team, operatorId, usernamesByAccountId))
                .ToArray(),
        };
    }

    public static async Task<IReadOnlyDictionary<string, string>> LoadUsernamesAsync(
        IAccountRepository accounts,
        IReadOnlyList<Team> teams)
    {
        var ids = CollectAccountIds(teams);
        if (ids.Length == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var rows = await accounts.AsQueryable()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Username })
            .ToArrayAsync();

        return rows.ToDictionary(
            row => row.Id,
            row => row.Username,
            StringComparer.Ordinal);
    }

    public static string[] CollectAccountIds(IReadOnlyList<Team> teams)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var team in teams)
        {
            if (!string.IsNullOrWhiteSpace(team.TeamLeaderId))
                ids.Add(team.TeamLeaderId);

            foreach (var operatorId in team.OperatorIds)
                ids.Add(operatorId);

            foreach (var rule in team.OperatorProfitShareRules)
            {
                foreach (var cut in rule.Cuts)
                {
                    if (!string.IsNullOrWhiteSpace(cut.AccountId))
                        ids.Add(cut.AccountId);
                }
            }
        }

        return ids.ToArray();
    }

    private static OperatorDetails MapOperator(
        Team team,
        string operatorId,
        IReadOnlyDictionary<string, string> usernamesByAccountId)
    {
        var rule = team.OperatorProfitShareRules
            .FirstOrDefault(r => string.Equals(r.OperatorId, operatorId, StringComparison.Ordinal));

        return new OperatorDetails
        {
            AccountId = operatorId,
            Username = ResolveUsername(usernamesByAccountId, operatorId),
            ProfitShareRule = new ProfitShareRuleDetails
            {
                Cuts = (rule?.Cuts ?? new List<ProfitSplitRecord>())
                    .Select(cut => new ProfitSplitDetails
                    {
                        AccountId = cut.AccountId,
                        Username = ResolveUsername(usernamesByAccountId, cut.AccountId),
                        Percentage = cut.Percentage,
                    })
                    .ToArray(),
            },
        };
    }

    internal static string ResolveUsername(
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        string accountId)
        => usernamesByAccountId.TryGetValue(accountId, out var username)
            ? username
            : accountId;
}
