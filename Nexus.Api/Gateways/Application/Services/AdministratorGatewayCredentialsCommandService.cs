using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class AdministratorGatewayCredentialsCommandService : IAdministratorGatewayCredentialsCommandService
{
    private IFrendzApiKeysService _frendzApiKeys { get; }
    private IWintechApiKeysService _wintechApiKeys { get; }
    private ISigiloPayApiKeysService _sigiloPayApiKeys { get; }

    public AdministratorGatewayCredentialsCommandService(
        IFrendzApiKeysService frendzApiKeys,
        IWintechApiKeysService wintechApiKeys,
        ISigiloPayApiKeysService sigiloPayApiKeys)
    {
        _frendzApiKeys = frendzApiKeys;
        _wintechApiKeys = wintechApiKeys;
        _sigiloPayApiKeys = sigiloPayApiKeys;
    }

    public Task<IResult<FrendzApiCredentials>> AddFrendzCredentialsAsync(AddCredentialsRequest request)
        => _frendzApiKeys.AddCredentialsAsync(request);

    public Task<IResult> UpdateFrendzCredentialsAsync(UpdateCredentialsRequest request)
        => _frendzApiKeys.UpdateCredentialsAsync(request);

    public Task<IResult> SetFrendzCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request)
        => _frendzApiKeys.SetCredentialEnabledAsync(request);

    public Task<IResult> DeleteFrendzCredentialsAsync(string id)
        => _frendzApiKeys.DeleteCredentialsAsync(id);

    public Task<IResult<WintechApiCredentials>> AddWintechCredentialsAsync(AddWintechCredentialsRequest request)
        => _wintechApiKeys.AddCredentialsAsync(request);

    public Task<IResult> UpdateWintechCredentialsAsync(UpdateWintechCredentialsRequest request)
        => _wintechApiKeys.UpdateCredentialsAsync(request);

    public Task<IResult> SetWintechCredentialEnabledAsync(SetWintechCredentialEnabledRequest request)
        => _wintechApiKeys.SetCredentialEnabledAsync(request);

    public Task<IResult> DeleteWintechCredentialsAsync(string id)
        => _wintechApiKeys.DeleteCredentialsAsync(id);

    public Task<IResult<SigiloPayApiCredentials>> AddSigiloPayCredentialsAsync(AddSigiloPayCredentialsRequest request)
        => _sigiloPayApiKeys.AddCredentialsAsync(request);

    public Task<IResult> UpdateSigiloPayCredentialsAsync(UpdateSigiloPayCredentialsRequest request)
        => _sigiloPayApiKeys.UpdateCredentialsAsync(request);

    public Task<IResult> SetSigiloPayCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request)
        => _sigiloPayApiKeys.SetCredentialEnabledAsync(request);

    public Task<IResult> DeleteSigiloPayCredentialsAsync(string id)
        => _sigiloPayApiKeys.DeleteCredentialsAsync(id);
}
