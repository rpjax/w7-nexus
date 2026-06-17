namespace Nexus.Authentications.Application.Services.Models;

public class JwtTokenSubject
{
    public string AccountId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
