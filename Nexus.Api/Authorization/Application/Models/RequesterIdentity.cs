using Nexus.Accounts.Aggregates;

namespace Nexus.Authorization.Application.Models;

public sealed record RequesterIdentity(
    string AccountId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions)
{
    public static RequesterIdentity FromAccount(Account account)
        => new(
            account.Id,
            account.Roles.ToArray(),
            account.Permissions.ToArray());
}
