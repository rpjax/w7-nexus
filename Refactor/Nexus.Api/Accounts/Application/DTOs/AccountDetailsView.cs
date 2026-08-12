using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

namespace Refactor.Nexus.Api.Accounts.Application.DTOs;

public sealed class AccountDetailsView
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Status { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }

    public static AccountDetailsView FromAccount(Account account) =>
        new()
        {
            Id = account.Id.ToString(),
            Username = account.Username,
            Status = account.Status.ToString(),
            Roles = account.Roles.ToArray(),
            Permissions = account.Permissions.ToArray(),
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };
}
