using Nexus.Authentications.Application.Services.Models;

namespace Nexus.Authentications.Application.Responses;

public class SignUpResponse
{
    public string AccountId { get; init; } = string.Empty;
    public AuthenticationTokens Tokens { get; init; } = new();
}
