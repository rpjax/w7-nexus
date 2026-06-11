using Nexus.Authentication.Application.Models;

namespace Nexus.Authentication.Services.Responses;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
