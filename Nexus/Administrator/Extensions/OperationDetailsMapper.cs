using Aidan.Core.Linq.Extensions;
using Nexus.Accounts.Application.Contracts;
using Nexus.Administrator.Application.Responses.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;

namespace Nexus.Administrator.Extensions;

public static class OperationDetailsMapper
{
    public static OperationDetails Map(
        Operation operation,
        IReadOnlyList<Team> teams,
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        TeamGatewayLookup? gatewayLookup = null)
    {
        var operationTeams = teams
            .Where(t => t.OperationId == operation.Id)
            .OrderBy(t => t.Name)
            .Select(t => TeamDetailsMapper.Map(t, usernamesByAccountId, gatewayLookup))
            .ToArray();

        return new OperationDetails
        {
            Id = operation.Id,
            Name = operation.Name,
            Description = operation.Description,
            Administrators = operation.AdministratorIds
                .Select(id => new OperationAdministratorDetails
                {
                    AccountId = id,
                    Username = ResolveUsername(usernamesByAccountId, id),
                })
                .ToArray(),
            Teams = operationTeams,
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
        };
    }

    public static async Task<IReadOnlyList<OperationDetails>> MapManyAsync(
        IReadOnlyList<Operation> operations,
        ITeamRepository teams,
        IAccountRepository accounts,
        ITeamGatewayDetailsLoader? gatewayLoader = null)
    {
        if (operations.Count == 0)
            return Array.Empty<OperationDetails>();

        var operationIds = operations.Select(o => o.Id).ToArray();

        var operationTeams = await teams.AsQueryable()
            .Where(t => operationIds.Contains(t.OperationId))
            .ToArrayAsync();

        var usernames = await LoadUsernamesAsync(accounts, CollectAccountIds(operations, operationTeams));
        var gatewayLookup = gatewayLoader is null
            ? new TeamGatewayLookup()
            : await gatewayLoader.LoadAsync(operationTeams);

        return operations
            .Select(o => Map(o, operationTeams, usernames, gatewayLookup))
            .ToList();
    }

    public static string[] CollectAccountIds(
        IReadOnlyList<Operation> operations,
        IReadOnlyList<Team> teams)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in operations)
        {
            foreach (var administratorId in operation.AdministratorIds)
                ids.Add(administratorId);
        }

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

    public static async Task<IReadOnlyDictionary<string, string>> LoadUsernamesAsync(
        IAccountRepository accounts,
        string[] accountIds)
    {
        if (accountIds.Length == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var rows = await accounts.AsQueryable()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Username })
            .ToArrayAsync();

        return rows.ToDictionary(
            row => row.Id,
            row => row.Username,
            StringComparer.Ordinal);
    }

    internal static string ResolveUsername(
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        string accountId)
        => usernamesByAccountId.TryGetValue(accountId, out var username)
            ? username
            : accountId;
}
