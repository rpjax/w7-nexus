using Nexus.Authentication.Application.Models;

namespace Nexus.Authentication.Application.Contracts;

public interface IJwtTokenService
{
    AuthenticationTokens GenerateTokens(JwtTokenSubject subject);
    JwtTokenSubject? ValidateAccessToken(string accessToken);
    JwtTokenSubject? ValidateRefreshToken(string refreshToken);
    AuthenticationTokens RefreshTokens(string refreshToken);
}
