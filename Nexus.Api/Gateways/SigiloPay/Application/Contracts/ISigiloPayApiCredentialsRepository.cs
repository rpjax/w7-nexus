using Aidan.Core.Patterns;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Application.Contracts;

public interface ISigiloPayApiCredentialsRepository : IRepository<SigiloPayApiCredentials>
{
    new Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity);
}
