using Nexus.Accounts.Application.Contracts;
using Nexus.Operations.Aggregates;
using Nexus.TeamLeaders.Application.Responses.Models;

namespace Nexus.TeamLeaders.Application.Mapping;

public static class OperationWithLedTeamsDetailsMapper
{
    public static async Task<IReadOnlyList<OperationWithLedTeamsDetails>> MapManyAsync(
        IReadOnlyList<Operation> operations,
        IReadOnlyList<Team> ledTeams,
        IAccountRepository accounts)
    {
        if (operations.Count == 0)
            return Array.Empty<OperationWithLedTeamsDetails>();

        var usernames = await TeamDetailsMapper.LoadUsernamesAsync(accounts, ledTeams);

        return operations
            .Select(operation =>
            {
                var operationTeams = ledTeams
                    .Where(t => t.OperationId == operation.Id)
                    .OrderBy(t => t.Name)
                    .Select(t => TeamDetailsMapper.Map(t, usernames))
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
