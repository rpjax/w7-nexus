using Aidan.Core.Linq.Extensions;
using Nexus.Accounts.Application.Contracts;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.TeamLeader.Application.Responses.Models;

namespace Nexus.TeamLeader.Extensions;

public static class TeamDetailsMapper
{
    public static TeamDetails Map(
        Team team,
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        TeamGatewayLookup? gatewayLookup = null)
    {
        gatewayLookup ??= new TeamGatewayLookup();

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
            GatewaySelectionStrategy = team.GatewaySelectionStrategy.ToString(),
            StrawMen = team.StrawManIds
                .Select(id => new TeamAccountDetails
                {
                    AccountId = id,
                    Username = ResolveUsername(usernamesByAccountId, id),
                })
                .ToArray(),
            GatewayCredentials = team.GatewayCredentialsIds
                .Select(id => gatewayLookup.CredentialsById.TryGetValue(id, out var credential)
                    ? credential
                    : new TeamGatewayCredentialDetails { Id = id, Name = id, Gateway = "desconhecido" })
                .ToArray(),
            GatewayCredentialsGroups = team.GatewayCredentialsGroupIds
                .Select(id => gatewayLookup.GroupsById.TryGetValue(id, out var group)
                    ? group
                    : new TeamGatewayGroupDetails { Id = id, Name = id, CredentialCount = 0 })
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

            foreach (var strawManId in team.StrawManIds)
                ids.Add(strawManId);

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

public static class OperationWithLedTeamsDetailsMapper
{
    public static async Task<IReadOnlyList<OperationWithLedTeamsDetails>> MapManyAsync(
        IReadOnlyList<Operation> operations,
        IReadOnlyList<Team> ledTeams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader? gatewayLoader = null)
    {
        if (operations.Count == 0)
            return Array.Empty<OperationWithLedTeamsDetails>();

        var usernames = await TeamDetailsMapper.LoadUsernamesAsync(accounts, ledTeams);
        var gatewayLookup = gatewayLoader is null
            ? new TeamGatewayLookup()
            : await gatewayLoader.LoadAsync(ledTeams);

        return operations
            .Select(operation =>
            {
                var operationTeams = ledTeams
                    .Where(t => t.OperationId == operation.Id)
                    .OrderBy(t => t.Name)
                    .Select(t => TeamDetailsMapper.Map(t, usernames, gatewayLookup))
                    .ToArray();

                return new OperationWithLedTeamsDetails
                {
                    Id = operation.Id,
                    Name = operation.Name,
                    Description = operation.Description,
                    Teams = operationTeams,
                    CreatedAt = operation.CreatedAt,
                    UpdatedAt = operation.UpdatedAt,
                };
            })
            .ToList();
    }
}
