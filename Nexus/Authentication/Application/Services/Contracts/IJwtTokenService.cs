using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Services.Contracts;

public interface IJwtTokenService
{
    AuthenticationTokens GenerateTokens(JwtTokenSubject subject);
    JwtTokenSubject? ValidateAccessToken(string accessToken);
    JwtTokenSubject? ValidateRefreshToken(string refreshToken);
    AuthenticationTokens RefreshTokens(string refreshToken);
}
