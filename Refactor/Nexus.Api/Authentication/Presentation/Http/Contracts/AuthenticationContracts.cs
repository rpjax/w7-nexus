using Refactor.Nexus.Api.Authentication.Application.Models;

namespace Refactor.Nexus.Api.Authentication.Presentation.Http.Contracts;

public sealed class SignUpRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class SignInRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ChangeMyPasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangeMyUsernameRequest
{
    public string NewUsername { get; set; } = string.Empty;
}

public sealed class SignUpResponse
{
    public string AccountId { get; init; } = string.Empty;
    public AuthenticationTokens Tokens { get; init; } = new();
}

public sealed class SignInResponse
{
    public AuthenticationTokens Tokens { get; init; } = new();
}

public sealed class ChangeMyPasswordResponse
{
    public required AuthenticationTokens Tokens { get; init; }
}

public sealed class ChangeMyUsernameResponse
{
    public required string Username { get; init; }
}

public sealed class MyProfileResponse
{
    public required MyProfileView Profile { get; init; }
}
