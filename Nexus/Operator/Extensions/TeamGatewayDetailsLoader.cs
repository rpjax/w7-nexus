using Aidan.Core.Linq.Extensions;
using Nexus.Operator.Application.Responses.Models;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Operations.Aggregates;

namespace Nexus.Operator.Extensions;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(IReadOnlyList<Team> teams, CancellationToken cancellationToken = default);
}

public sealed class TeamGatewayLookup
{
    public IReadOnlyDictionary<string, TeamGatewayCredentialDetails> CredentialsById { get; init; }
        = new Dictionary<string, TeamGatewayCredentialDetails>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TeamGatewayGroupDetails> GroupsById { get; init; }
        = new Dictionary<string, TeamGatewayGroupDetails>(StringComparer.Ordinal);
}

public sealed class TeamGatewayDetailsLoader : ITeamGatewayDetailsLoader
{
    private IFrendzApiCredentialsRepository _frendzCredentials { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayCredentials { get; }
    private IWintechApiCredentialsRepository _wintechCredentials { get; }
    private IGatewayCredentialsGroupRepository _gatewayGroups { get; }

    public TeamGatewayDetailsLoader(
        IFrendzApiCredentialsRepository frendzCredentials,
        ISigiloPayApiCredentialsRepository sigiloPayCredentials,
        IWintechApiCredentialsRepository wintechCredentials,
        IGatewayCredentialsGroupRepository gatewayGroups)
    {
        _frendzCredentials = frendzCredentials;
        _sigiloPayCredentials = sigiloPayCredentials;
        _wintechCredentials = wintechCredentials;
        _gatewayGroups = gatewayGroups;
    }

    public async Task<TeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        CancellationToken cancellationToken = default)
    {
        if (teams.Count == 0)
            return new TeamGatewayLookup();

        var credentialIds = teams
            .SelectMany(t => t.GatewayCredentialsIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var groupIds = teams
            .SelectMany(t => t.GatewayCredentialsGroupIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var credentialsById = await LoadCredentialsAsync(credentialIds, cancellationToken);
        var groupsById = await LoadGroupsAsync(groupIds, cancellationToken);

        return new TeamGatewayLookup
        {
            CredentialsById = credentialsById,
            GroupsById = groupsById,
        };
    }

    private async Task<IReadOnlyDictionary<string, TeamGatewayCredentialDetails>> LoadCredentialsAsync(
        string[] credentialIds,
        CancellationToken cancellationToken)
    {
        if (credentialIds.Length == 0)
            return new Dictionary<string, TeamGatewayCredentialDetails>(StringComparer.Ordinal);

        var result = new Dictionary<string, TeamGatewayCredentialDetails>(StringComparer.Ordinal);

        var frendz = await _frendzCredentials.AsQueryable()
            .Where(c => credentialIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToArrayAsync();

        foreach (var row in frendz)
        {
            result[row.Id] = new TeamGatewayCredentialDetails
            {
                Id = row.Id,
                Name = row.Name,
                Gateway = "frendz",
            };
        }

        var sigilo = await _sigiloPayCredentials.AsQueryable()
            .Where(c => credentialIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToArrayAsync();

        foreach (var row in sigilo)
        {
            result[row.Id] = new TeamGatewayCredentialDetails
            {
                Id = row.Id,
                Name = row.Name,
                Gateway = "sigilopay",
            };
        }

        var wintech = await _wintechCredentials.AsQueryable()
            .Where(c => credentialIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToArrayAsync();

        foreach (var row in wintech)
        {
            result[row.Id] = new TeamGatewayCredentialDetails
            {
                Id = row.Id,
                Name = row.Name,
                Gateway = "wintech",
            };
        }

        foreach (var id in credentialIds)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = new TeamGatewayCredentialDetails
                {
                    Id = id,
                    Name = id,
                    Gateway = "desconhecido",
                };
            }
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<string, TeamGatewayGroupDetails>> LoadGroupsAsync(
        string[] groupIds,
        CancellationToken cancellationToken)
    {
        if (groupIds.Length == 0)
            return new Dictionary<string, TeamGatewayGroupDetails>(StringComparer.Ordinal);

        var groups = await _gatewayGroups.AsQueryable()
            .Where(g => groupIds.Contains(g.Id))
            .ToArrayAsync();

        return groups.ToDictionary(
            group => group.Id,
            group => new TeamGatewayGroupDetails
            {
                Id = group.Id,
                Name = group.Name,
                CredentialCount = group.GatewayCredentialsIds.Count,
            },
            StringComparer.Ordinal);
    }
}
