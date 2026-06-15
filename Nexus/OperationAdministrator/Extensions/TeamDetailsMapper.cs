using Nexus.OperationAdministrator.Application.Responses.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.OperationAdministrator.Extensions;

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
}
