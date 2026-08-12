namespace Refactor.Nexus.Api.Authentication.Application.Models;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "refactor-nexus";
    public string Audience { get; init; } = "refactor-nexus";
    public string SecretKey { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
