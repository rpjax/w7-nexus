using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayCredentialsIdValidator
{
    Task<bool> ExistsAsync(string credentialsId);
}
