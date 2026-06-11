namespace Nexus.Authentication.Application.Models;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
