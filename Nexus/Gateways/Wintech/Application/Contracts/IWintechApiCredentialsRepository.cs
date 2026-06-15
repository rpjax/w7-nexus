using Aidan.Core.Patterns;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Wintech.Application.Contracts;

public interface IWintechApiCredentialsRepository : IRepository<WintechApiCredentials>
{
    new Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity);
}
