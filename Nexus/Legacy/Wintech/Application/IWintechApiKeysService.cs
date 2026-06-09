using Aidan.Core.Patterns;
using Nexus.Legacy.Wintech.Application.Models;

namespace Nexus.Legacy.Wintech.Application;

public interface IWintechApiKeysService
{
    Task<WintechApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<WintechApiCredentials>> AddCredentialsAsync(AddWintechCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateWintechCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetWintechCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
