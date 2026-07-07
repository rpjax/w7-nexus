using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IAdministratorGatewayCredentialsCommandService
{
    Task<IResult<FrendzApiCredentials>> AddFrendzCredentialsAsync(AddCredentialsRequest request);
    Task<IResult> UpdateFrendzCredentialsAsync(UpdateCredentialsRequest request);
    Task<IResult> SetFrendzCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request);
    Task<IResult> DeleteFrendzCredentialsAsync(string id);

    Task<IResult<WintechApiCredentials>> AddWintechCredentialsAsync(AddWintechCredentialsRequest request);
    Task<IResult> UpdateWintechCredentialsAsync(UpdateWintechCredentialsRequest request);
    Task<IResult> SetWintechCredentialEnabledAsync(SetWintechCredentialEnabledRequest request);
    Task<IResult> DeleteWintechCredentialsAsync(string id);

    Task<IResult<SigiloPayApiCredentials>> AddSigiloPayCredentialsAsync(AddSigiloPayCredentialsRequest request);
    Task<IResult> UpdateSigiloPayCredentialsAsync(UpdateSigiloPayCredentialsRequest request);
    Task<IResult> SetSigiloPayCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request);
    Task<IResult> DeleteSigiloPayCredentialsAsync(string id);
}
