using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;

namespace Refactor.Nexus.Api.Authentication.Infrastructure.Tokens;

public sealed class JwtTokenService : IJwtTokenService
{
    private const string TokenTypeClaim = "token_type";
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";
    private const string RoleClaimType = "role";
    private const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _options = new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"] ?? "refactor-nexus",
            Audience = configuration["Jwt:Audience"] ?? "refactor-nexus",
            SecretKey = configuration["Jwt:SigningKey"] ?? string.Empty,
            AccessTokenExpirationMinutes = configuration.GetValue<int?>("Jwt:AccessTokenExpirationMinutes") ?? 60,
            RefreshTokenExpirationDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpirationDays") ?? 30
        };

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured with at least 32 characters.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
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
            claims.Add(new Claim(RoleClaimType, role));

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
}
