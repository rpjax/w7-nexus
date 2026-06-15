using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Services;

public sealed class GatewayCredentialsIdValidator : IGatewayCredentialsIdValidator
{
    private readonly IFrendzApiCredentialsRepository _frendzCredentials;
    private readonly ISigiloPayApiCredentialsRepository _sigiloPayCredentials;
    private readonly IWintechApiCredentialsRepository _wintechCredentials;

    public GatewayCredentialsIdValidator(
        IFrendzApiCredentialsRepository frendzCredentials,
        ISigiloPayApiCredentialsRepository sigiloPayCredentials,
        IWintechApiCredentialsRepository wintechCredentials)
    {
        _frendzCredentials = frendzCredentials;
        _sigiloPayCredentials = sigiloPayCredentials;
        _wintechCredentials = wintechCredentials;
    }

    public Task<bool> ExistsAsync(string credentialsId)
    {
        var normalized = credentialsId.Trim();

        var exists = _frendzCredentials.AsQueryable().Any(c => c.Id == normalized)
            || _sigiloPayCredentials.AsQueryable().Any(c => c.Id == normalized)
            || _wintechCredentials.AsQueryable().Any(c => c.Id == normalized);

        return Task.FromResult(exists);
    }
}
