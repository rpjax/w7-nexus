using Microsoft.Extensions.Options;
using Nexus.Authentication.Application.Services.Contracts;
using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Authentication.Application.Services;

public sealed class AdministratorSignUpTokenService : IAdministratorSignUpTokenService
{
    private readonly AuthenticationOptions _options;

    public AdministratorSignUpTokenService(IOptions<AuthenticationOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAuthorized(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(_options.AdministratorToken))
            return false;

        return string.Equals(authorizationHeader, _options.AdministratorToken, StringComparison.Ordinal);
    }
}
