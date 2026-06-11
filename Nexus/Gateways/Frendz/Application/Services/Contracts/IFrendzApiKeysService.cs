using Aidan.Core.Patterns;
using Nexus.Gateways.Frendz.Application.Services.Contracts;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application.Services.Contracts;

public interface IFrendzApiKeysService
{
    Task<FrendzApiCredentials?> GetRandomCredentialsAsync();
    Task<IResult<FrendzApiCredentials>> AddCredentialsAsync(AddCredentialsRequest request);
    Task<IResult> UpdateCredentialsAsync(UpdateCredentialsRequest request);
    Task<IResult> SetCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request);
    Task<IResult> DeleteCredentialsAsync(string id);
}
