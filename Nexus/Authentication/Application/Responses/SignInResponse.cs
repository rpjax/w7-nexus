using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Responses;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
