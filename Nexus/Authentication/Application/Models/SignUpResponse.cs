namespace Nexus.Authentication.Application.Models;

public class SignUpResponse
{
    public string AccountId { get; init; } = string.Empty;
    public AuthenticationTokens Tokens { get; init; } = new();
}
