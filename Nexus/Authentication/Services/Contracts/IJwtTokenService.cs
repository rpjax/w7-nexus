using Nexus.Authentication.Services.Models;

namespace Nexus.Authentication.Services.Contracts;

public interface IJwtTokenService
{
    AuthenticationTokens GenerateTokens(JwtTokenSubject subject);
    JwtTokenSubject? ValidateAccessToken(string accessToken);
    JwtTokenSubject? ValidateRefreshToken(string refreshToken);
    AuthenticationTokens RefreshTokens(string refreshToken);
}
