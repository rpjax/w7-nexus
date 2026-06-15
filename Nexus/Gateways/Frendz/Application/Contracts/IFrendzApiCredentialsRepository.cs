using Aidan.Core.Patterns;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application.Services.Contracts;

public interface IFrendzApiCredentialsRepository : IRepository<FrendzApiCredentials>
{
    new Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity);
}
