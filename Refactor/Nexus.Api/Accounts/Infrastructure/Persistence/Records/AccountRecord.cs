namespace Refactor.Nexus.Api.Accounts.Infrastructure.Persistence.Records;

public sealed class AccountRecord
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public required string Status { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastUpdatedAt { get; init; }
}
