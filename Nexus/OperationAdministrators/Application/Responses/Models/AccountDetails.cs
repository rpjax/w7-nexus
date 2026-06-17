namespace Nexus.OperationAdministrators.Application.Responses.Models;

public class AccountDetails
{
    public string Id { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
