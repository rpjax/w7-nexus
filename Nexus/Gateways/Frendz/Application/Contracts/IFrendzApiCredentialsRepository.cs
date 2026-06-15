using Aidan.Core.Patterns;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application.Contracts;

public interface IFrendzApiCredentialsRepository : IRepository<FrendzApiCredentials>
{
    new Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity);
}
