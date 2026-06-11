using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nexus.Authentication.Application.Services.Contracts;
using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private const string TokenTypeClaim = "token_type";
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";
    private const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must be configured with at least 32 characters.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        _validationParameters = CreateValidationParameters();
    }

    public AuthenticationTokens GenerateTokens(JwtTokenSubject subject)
    {
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        return new AuthenticationTokens
        {
            AccessToken = CreateToken(subject, accessExpiresAt, AccessTokenType),
            RefreshToken = CreateToken(subject, refreshExpiresAt, RefreshTokenType),
            ExpiresAt = accessExpiresAt,
            TokenType = "Bearer"
        };
    }

    public JwtTokenSubject? ValidateAccessToken(string accessToken)
    {
        return ValidateToken(accessToken, AccessTokenType);
    }

    public JwtTokenSubject? ValidateRefreshToken(string refreshToken)
    {
        return ValidateToken(refreshToken, RefreshTokenType);
    }

    public AuthenticationTokens RefreshTokens(string refreshToken)
    {
        var subject = ValidateRefreshToken(refreshToken)
            ?? throw new SecurityTokenException("Refresh token is invalid or expired.");

        return GenerateTokens(subject);
    }

    private string CreateToken(JwtTokenSubject subject, DateTime expiresAt, string tokenType)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.AccountId),
            new(JwtRegisteredClaimNames.UniqueName, subject.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenTypeClaim, tokenType)
        };

        foreach (var role in subject.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in subject.Permissions)
            claims.Add(new Claim(PermissionClaimType, permission));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private JwtTokenSubject? ValidateToken(string token, string expectedTokenType)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken
                || !string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            var tokenType = principal.FindFirst(TokenTypeClaim)?.Value;
            if (!string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal))
                return null;

            var accountId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(username))
                return null;

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var permissions = principal.FindAll(PermissionClaimType).Select(c => c.Value).ToArray();

            return new JwtTokenSubject
            {
                AccountId = accountId,
                Username = username,
                Roles = roles,
                Permissions = permissions
            };
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }
}
