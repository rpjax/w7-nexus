using Aidan.Core.Patterns;
using Nexus.Frendz.Application.Models;

namespace Nexus.Frendz.Application;

public interface IFrendzApiKeysService
{
    Task<FrendzApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<FrendzApiCredentials>> AddCredentialsAsync(AddCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
