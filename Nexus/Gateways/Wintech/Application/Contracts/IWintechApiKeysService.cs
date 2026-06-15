using Aidan.Core.Patterns;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Wintech.Application.Contracts;

public interface IWintechApiKeysService
{
    Task<WintechApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<WintechApiCredentials>> AddCredentialsAsync(AddWintechCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateWintechCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetWintechCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
