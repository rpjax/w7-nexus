using Nexus.Authentication.Services.Models;

namespace Nexus.Authentication.Services.Responses;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
