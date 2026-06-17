using Nexus.Authentications.Application.Services.Models;

namespace Nexus.Authentications.Application.Responses;

public class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}
