using System.Collections.Generic;
using Nexus.Frendz.Application.Models;

namespace Nexus.Frendz.Application;

public interface IFrendzApiKeysService
{
    Task<FrendzApiCredentials?> GetRandomCredentialsAsync();
    Task<FrendzApiCredentials> AddCredentialsAsync(string? strawManId, string token, string name);
    Task<bool> UpdateCredentialsAsync(string id, string? strawManId, string token, string name);
    Task<bool> DeleteCredentialsAsync(string id);
}
