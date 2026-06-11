using Nexus.Authentication.Application.Models;

namespace Nexus.Authentication.Services.Responses;

public class SignUpResponse
{
    public string AccountId { get; init; } = string.Empty;
    public AuthenticationTokens Tokens { get; init; } = new();
}
