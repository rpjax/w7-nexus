using Aidan.Core.Patterns;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Application.Contracts;

public interface ISigiloPayApiCredentialsRepository : IRepository<SigiloPayApiCredentials>
{
}
