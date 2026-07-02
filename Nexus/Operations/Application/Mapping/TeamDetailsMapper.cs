using Nexus.Operations.Application.Models;
using Nexus.Operations.Application.Responses.Administrator.Models;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Mapping;

public static class TeamDetailsMapper
{
    public static TeamDetails Map(Team team)
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
                    Username = team.TeamLeaderId,
                },
            Operators = team.OperatorIds
                .Select(id => new OperatorDetails
                {
                    AccountId = id,
                    Username = id,
                })
                .ToArray(),
            GatewaySelectionStrategy = team.GatewaySelectionStrategy.ToString(),
            StrawMen = team.StrawManIds
                .Select(id => new TeamAccountDetails
                {
                    AccountId = id,
                    Username = id,
                })
                .ToArray(),
            GatewayCredentials = team.GatewayCredentialsIds
                .Select(id => new TeamGatewayCredentialDetails
                {
                    Id = id,
                    Name = id,
                    Gateway = "desconhecido",
                })
                .ToArray(),
            GatewayCredentialsGroups = team.GatewayCredentialsGroupIds
                .Select(id => new TeamGatewayGroupDetails
                {
                    Id = id,
                    Name = id,
                    CredentialCount = 0,
                })
                .ToArray(),
        };
    }

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
                    Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, team.TeamLeaderId),
                },
            Operators = team.OperatorIds
                .Select(operatorId => MapOperator(team, operatorId, usernamesByAccountId))
                .ToArray(),
            GatewaySelectionStrategy = team.GatewaySelectionStrategy.ToString(),
            StrawMen = team.StrawManIds
                .Select(id => new TeamAccountDetails
                {
                    AccountId = id,
                    Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, id),
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
            Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, operatorId),
            ProfitShareRule = new ProfitShareRuleDetails
            {
                Cuts = (rule?.Cuts ?? new List<ProfitSplitRecord>())
                    .Select(cut => new ProfitSplitDetails
                    {
                        AccountId = cut.AccountId,
                        Username = OperationDetailsMapper.ResolveUsername(usernamesByAccountId, cut.AccountId),
                        Percentage = cut.Percentage,
                    })
                    .ToArray(),
            },
        };
    }
}
