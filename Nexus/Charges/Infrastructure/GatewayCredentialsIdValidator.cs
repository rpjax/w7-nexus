using Nexus.Charges.Application;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.SigiloPay.Application;
using Nexus.Legacy.Wintech.Application;

namespace Nexus.Charges.Infrastructure;

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
