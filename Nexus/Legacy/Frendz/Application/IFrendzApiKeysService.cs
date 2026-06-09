using Aidan.Core.Patterns;
using Nexus.Legacy.Frendz.Application.Models;

namespace Nexus.Legacy.Frendz.Application;

public interface IFrendzApiKeysService
{
    Task<FrendzApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<FrendzApiCredentials>> AddCredentialsAsync(AddCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
