using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Services.Responses;

public class SignUpResponse
{
    public string AccountId { get; init; } = string.Empty;
    public AuthenticationTokens Tokens { get; init; } = new();
}
