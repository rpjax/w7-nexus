using Nexus.Authentications.Application.Services.Models;

namespace Nexus.Authentications.Application.Contracts;

public interface IJwtTokenService
{
    AuthenticationTokens GenerateTokens(JwtTokenSubject subject);
    JwtTokenSubject? ValidateAccessToken(string accessToken);
    JwtTokenSubject? ValidateRefreshToken(string refreshToken);
    AuthenticationTokens RefreshTokens(string refreshToken);
}
