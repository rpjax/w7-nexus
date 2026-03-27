using Aidan.Core.Patterns;
using Nexus.SigiloPay.Application.Models;

namespace Nexus.SigiloPay.Application;

public interface ISigiloPayApiKeysService
{
    Task<SigiloPayApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<SigiloPayApiCredentials>> AddCredentialsAsync(AddSigiloPayCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateSigiloPayCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
