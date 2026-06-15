using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Services.Responses;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
