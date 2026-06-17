using Aidan.Core.Linq.Extensions;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operators.Application.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operators.Application.Mapping;

public static class OperationDetailsMapper
{
    public static OperationDetails Map(
        Operation operation,
        Team team,
        IReadOnlyDictionary<string, string> usernamesByAccountId,
        string viewerOperatorAccountId)
    {
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
            Team = TeamDetailsMapper.Map(team, usernamesByAccountId, viewerOperatorAccountId),
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
        };
    }

    public static async Task<IReadOnlyList<OperationDetails>> MapManyAsync(
        IReadOnlyList<OperationTeamMembership> memberships,
        IAccountRepository accounts,
        string viewerOperatorAccountId)
    {
        if (memberships.Count == 0)
            return Array.Empty<OperationDetails>();

        var teams = memberships.Select(m => m.Team).ToArray();
        var operations = memberships.Select(m => m.Operation).ToArray();

        var usernames = await LoadUsernamesAsync(accounts, CollectAccountIds(operations, teams));

        return memberships
            .Select(m => Map(m.Operation, m.Team, usernames, viewerOperatorAccountId))
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

public readonly record struct OperationTeamMembership(Operation Operation, Team Team);
