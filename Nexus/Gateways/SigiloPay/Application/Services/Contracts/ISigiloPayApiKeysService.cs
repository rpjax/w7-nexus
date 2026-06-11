using Aidan.Core.Patterns;
using Nexus.Gateways.SigiloPay.Application.Services.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Application.Services.Contracts;

public interface ISigiloPayApiKeysService
{
    Task<SigiloPayApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<SigiloPayApiCredentials>> AddCredentialsAsync(AddSigiloPayCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateSigiloPayCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
