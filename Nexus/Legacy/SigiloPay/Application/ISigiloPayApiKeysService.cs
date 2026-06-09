using Aidan.Core.Patterns;
using Nexus.Legacy.SigiloPay.Application.Models;

namespace Nexus.Legacy.SigiloPay.Application;

public interface ISigiloPayApiKeysService
{
    Task<SigiloPayApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<SigiloPayApiCredentials>> AddCredentialsAsync(AddSigiloPayCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateSigiloPayCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
